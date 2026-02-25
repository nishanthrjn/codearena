// Placeholder for AuthService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CodeArena.Api.Data;
using CodeArena.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CodeArena.Api.Services;

public interface IAuthService
{
    Task<string> HandleCallbackAsync(string code, CancellationToken ct = default);
    Task<User?> GetUserAsync(Guid userId, CancellationToken ct = default);
}

public class AuthService(
    AppDbContext db,
    IConfiguration cfg,
    ITokenEncryptionService enc,
    IHttpClientFactory http,
    ILogger<AuthService> log) : IAuthService
{
    public async Task<string> HandleCallbackAsync(string code, CancellationToken ct = default)
    {
        // Exchange code for GitHub access token
        var client = http.CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "CodeArena");

        var tokenResp = await client.PostAsync(
            "https://github.com/login/oauth/access_token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = cfg["GitHub:ClientId"]!,
                ["client_secret"] = cfg["GitHub:ClientSecret"]!,
                ["code"] = code
            }), ct);

        tokenResp.EnsureSuccessStatusCode();
        var tokenJson = await tokenResp.Content.ReadAsStringAsync(ct);
        using var tokenDoc = JsonDocument.Parse(tokenJson);
        var ghToken = tokenDoc.RootElement.GetProperty("access_token").GetString()
                      ?? throw new InvalidOperationException("GitHub token missing from response");

        // Fetch GitHub user profile
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ghToken}");
        var userResp = await client.GetAsync("https://api.github.com/user", ct);
        userResp.EnsureSuccessStatusCode();
        var userJson = await userResp.Content.ReadAsStringAsync(ct);
        using var userDoc = JsonDocument.Parse(userJson);

        var ghId = userDoc.RootElement.GetProperty("id").GetInt64();
        var login = userDoc.RootElement.GetProperty("login").GetString() ?? "";
        var avatar = userDoc.RootElement.GetProperty("avatar_url").GetString() ?? "";
        var email = userDoc.RootElement.TryGetProperty("email", out var ep) ? ep.GetString() ?? "" : "";

        // Upsert user
        var user = await db.Users.FirstOrDefaultAsync(u => u.GitHubId == ghId, ct);
        if (user is null)
        {
            user = new User { GitHubId = ghId, Login = login, Email = email, AvatarUrl = avatar };
            db.Users.Add(user);
            log.LogInformation("New user registered: {Login}", login);
        }
        else
        {
            user.Login = login;
            user.AvatarUrl = avatar;
            user.Email = email;
        }
        user.EncryptedToken = enc.Encrypt(ghToken);
        await db.SaveChangesAsync(ct);

        return GenerateJwt(user);
    }

    public Task<User?> GetUserAsync(Guid userId, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim("github_login",                user.Login),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: cfg["Jwt:Issuer"],
            audience: cfg["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}