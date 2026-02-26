// Placeholder for AuthController.cs
using CodeArena.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeArena.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth, IConfiguration cfg, ILogger<AuthController> log) : ControllerBase
{
    // M-4: hard limits on OAuth parameters to prevent log flooding / abuse
    private const int MaxParamLength = 512;

    [HttpGet("login")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        var clientId    = cfg["GitHub:ClientId"]!;
        var redirectUri = cfg["GitHub:RedirectUri"]!;

        // H-3: validate returnUrl against AllowedOrigins before embedding in state
        var allowedOrigins = cfg["AllowedOrigins"]?.Split(',') ?? ["http://localhost:5173"];
        var safeReturn = IsAllowedReturnUrl(returnUrl, allowedOrigins) ? returnUrl : "/";

        // Bundle a nonce AND the validated returnUrl into the state parameter
        var nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        var statePayload = $"{nonce}|{safeReturn}";
        var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(statePayload));

        Response.Cookies.Append("oauth_state", state, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,   // Lax required for cross-site OAuth redirects
            Secure   = true,
            MaxAge   = TimeSpan.FromMinutes(10)
        });

        var url = $"https://github.com/login/oauth/authorize?client_id={clientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&scope=repo,read:user,user:email&state={Uri.EscapeDataString(state)}";
        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state)
    {
        // M-4: guard against oversized parameters
        if (code.Length > MaxParamLength || state.Length > MaxParamLength)
            return BadRequest("Invalid OAuth parameters");

        // Validate state cookie
        var stored = Request.Cookies["oauth_state"];
        if (string.IsNullOrEmpty(stored) || stored != state)
            return BadRequest("Invalid OAuth state");

        Response.Cookies.Delete("oauth_state");

        // Decode the state payload to recover the safe returnUrl
        string returnUrl = "/";
        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
            var pipe    = decoded.IndexOf('|');
            if (pipe >= 0) returnUrl = decoded[(pipe + 1)..];
        }
        catch
        {
            // Malformed state — just land on home page
        }

        // Exchange code for JWT and set as HttpOnly, Secure cookie (C-1)
        var jwt      = await auth.HandleCallbackAsync(code);
        var isSecure = Request.IsHttps;
        Response.Cookies.Append("ca_jwt", jwt, new CookieOptions
        {
            HttpOnly = true,
            Secure   = isSecure,
            SameSite = SameSiteMode.Strict,
            MaxAge   = TimeSpan.FromHours(8),   // matches JWT lifetime
            Path     = "/"
        });

        // Redirect to frontend WITHOUT token in URL (C-1 fix)
        var frontend = cfg["AllowedOrigins"]?.Split(',').First() ?? "http://localhost:5173";
        var safeReturnUrl = IsAllowedReturnUrl(returnUrl, [frontend]) ? returnUrl : "/";
        return Redirect($"{frontend}{safeReturnUrl}");
    }

    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("ca_jwt", new CookieOptions { Path = "/" });
        return NoContent();
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Me([FromServices] IAuthService svc)
    {
        var userId = GetUserId();
        var user   = await svc.GetUserAsync(userId);
        if (user is null) return NotFound();
        return Ok(new { user.Id, user.Login, user.Email, user.AvatarUrl });
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static bool IsAllowedReturnUrl(string? url, IEnumerable<string> allowedOrigins)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        // H-1: block path-injection characters that some parsers treat as authority separators
        if (url.Contains('\\') || url.Contains("%2f", StringComparison.OrdinalIgnoreCase)
                                || url.Contains("%5c", StringComparison.OrdinalIgnoreCase))
            return false;

        // Must be a relative path starting with / (no scheme = not an open redirect)
        // Reject // which is scheme-relative (i.e., //evil.com)
        if (url.StartsWith('/') && !url.StartsWith("//"))
        {
            // Only allow safe path characters
            return url.All(c => char.IsLetterOrDigit(c) || c is '/' or '-' or '_' or '.' or '~' or '?' or '=' or '&' or '#');
        }

        // Or an absolute URL whose origin is explicitly whitelisted
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return allowedOrigins.Any(o => string.Equals(o.TrimEnd('/'),
                $"{uri.Scheme}://{uri.Authority}", StringComparison.OrdinalIgnoreCase));
        return false;
    }

    // M-2: null-safe; throws UnauthorizedAccessException (caught by ExceptionMiddleware) instead of NullReferenceException
    private Guid GetUserId()
    {
        var val = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(val, out var id))
            throw new UnauthorizedAccessException("Invalid user identity in token.");
        return id;
    }
}