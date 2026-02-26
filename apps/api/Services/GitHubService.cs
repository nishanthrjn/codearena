// Placeholder for GitHubService.cs
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CodeArena.Api.Data;
using CodeArena.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace CodeArena.Api.Services;

public interface IGitHubService
{
    Task<List<GitHubRepoDto>> ListReposAsync(Guid userId, CancellationToken ct = default);
    Task<string> PushSnippetAsync(Guid userId, PushToGitHubRequest req, CancellationToken ct = default);
}

public class GitHubService(
    AppDbContext db,
    ITokenEncryptionService enc,
    IHttpClientFactory httpFactory,
    IConnectionMultiplexer redis,
    ILogger<GitHubService> log) : IGitHubService
{
    // M-5: cache key for per-user repo list, TTL 60s to reduce GitHub API calls
    private static string RepoCacheKey(Guid userId) => $"codearena:repos:{userId}";
    public async Task<List<GitHubRepoDto>> ListReposAsync(Guid userId, CancellationToken ct = default)
    {
        // M-5: serve from Redis cache if available (TTL 60s)
        var db2 = redis.GetDatabase();
        var cached = await db2.StringGetAsync(RepoCacheKey(userId));
        if (cached.HasValue)
        {
            var cachedList = System.Text.Json.JsonSerializer.Deserialize<List<GitHubRepoDto>>(cached!);
            if (cachedList is not null) return cachedList;
        }

        var client = await BuildClientAsync(userId, ct);
        var resp = await client.GetAsync("https://api.github.com/user/repos?per_page=100&sort=updated", ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var repos = doc.RootElement.EnumerateArray().Select(r => new GitHubRepoDto(
            r.GetProperty("full_name").GetString() ?? "",
            r.GetProperty("default_branch").GetString() ?? "main",
            r.GetProperty("private").GetBoolean())).ToList();

        // Cache result for 60 seconds
        await db2.StringSetAsync(RepoCacheKey(userId),
            System.Text.Json.JsonSerializer.Serialize(repos),
            TimeSpan.FromSeconds(60));

        return repos;
    }

    public async Task<string> PushSnippetAsync(Guid userId, PushToGitHubRequest req, CancellationToken ct = default)
    {
        var client = await BuildClientAsync(userId, ct);

        // H-1: Verify the target repo actually belongs to this user
        var userRepos = await ListReposAsync(userId, ct);
        if (!userRepos.Any(r => string.Equals(r.FullName, req.RepoFullName, StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException($"Repository '{req.RepoFullName}' is not accessible to this user.");

        var snippet = await db.Snippets
            .Include(s => s.TestCases.OrderBy(t => t.OrderIndex))
            .FirstOrDefaultAsync(s => s.Id == req.SnippetId && s.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Snippet not found");

        var lang = snippet.Language;
        var ext = LanguageExtension(lang);
        var basePath = $"snippets/{lang}/{snippet.Slug}";

        // Build files: code, test inputs, metadata
        var files = new Dictionary<string, string>
        {
            [$"{basePath}/solution{ext}"] = snippet.Code,
            [$"{basePath}/metadata.json"] = JsonSerializer.Serialize(new
            {
                title = snippet.Title,
                language = lang,
                tags = snippet.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries),
                created = snippet.CreatedAt,
                updated = snippet.UpdatedAt
            }, new JsonSerializerOptions { WriteIndented = true }),
        };

        for (var i = 0; i < snippet.TestCases.Count; i++)
        {
            var tc = snippet.TestCases.ElementAt(i);
            files[$"{basePath}/tests/case{i + 1}_input.txt"] = tc.StdIn;
            files[$"{basePath}/tests/case{i + 1}_expected.txt"] = tc.Expected;
        }

        // Commit each file via GitHub Contents API
        var commitSha = "";
        foreach (var (path, content) in files)
        {
            var sha = await GetFileShaAsync(client, req.RepoFullName, path, req.Branch, ct);
            var payload = new Dictionary<string, object>
            {
                ["message"] = req.CommitMessage,
                ["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
                ["branch"] = req.Branch
            };
            if (sha is not null) payload["sha"] = sha;

            var putResp = await client.PutAsync(
                $"https://api.github.com/repos/{req.RepoFullName}/contents/{path}",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            putResp.EnsureSuccessStatusCode();
            log.LogInformation("Pushed {Path} to {Repo}", path, req.RepoFullName);

            var putJson = await putResp.Content.ReadAsStringAsync(ct);
            using var putDoc = JsonDocument.Parse(putJson);
            commitSha = putDoc.RootElement.GetProperty("commit").GetProperty("sha").GetString() ?? "";
        }
        return commitSha;
    }

    private async Task<string?> GetFileShaAsync(HttpClient client, string repo, string path, string branch, CancellationToken ct)
    {
        var resp = await client.GetAsync(
            $"https://api.github.com/repos/{repo}/contents/{path}?ref={branch}", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("sha").GetString();
    }

    private async Task<HttpClient> BuildClientAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new UnauthorizedAccessException("User not found");
        string token;
        try
        {
            token = enc.Decrypt(user.EncryptedToken);
        }
        catch (TokenDecryptionException)
        {
            // L-4: Data Protection keys have rotated or token is corrupted — force re-auth
            throw new UnauthorizedAccessException(
                "GitHub token could not be decrypted. Please disconnect and reconnect your GitHub account.");
        }
        var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("User-Agent", "CodeArena/1.0");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private static string LanguageExtension(string lang) => lang switch
    {
        "csharp" => ".cs",
        "python" => ".py",
        "javascript" => ".js",
        "c" => ".c",
        "cpp" => ".cpp",
        _ => ".txt"
    };
}