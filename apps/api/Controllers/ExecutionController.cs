// Placeholder for ExecutionController.cs
using CodeArena.Api.DTOs;
using CodeArena.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeArena.Api.Controllers;

[ApiController]
[Route("api/execution")]
[Authorize]
public class ExecutionController(IExecutionService svc) : ControllerBase
{
    [HttpPost("run")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("execution")]
    public async Task<IActionResult> Run([FromBody] RunRequest req, CancellationToken ct)
    {
        if (!AllowedLanguages.Contains(req.Language)) return BadRequest("Unsupported language");
        if (req.Code.Length > 100_000) return BadRequest("Code too large");
        if (req.StdIn.Length > 32_768) return BadRequest("Stdin too large");
        var jobId = await svc.EnqueueRunAsync(UserId, req, ct);
        return Accepted(new JobSubmittedDto(jobId, "queued"));
    }

    [HttpPost("test")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("execution")]
    public async Task<IActionResult> Test([FromBody] TestRequest req, CancellationToken ct)
    {
        if (!AllowedLanguages.Contains(req.Language)) return BadRequest("Unsupported language");
        if (req.TestCases.Count > 10) return BadRequest("Max 10 test cases");
        var jobId = await svc.EnqueueTestAsync(UserId, req, ct);
        return Accepted(new JobSubmittedDto(jobId, "queued"));
    }

    [HttpGet("result/{jobId:guid}")]
    public async Task<IActionResult> Result(Guid jobId, CancellationToken ct)
    {
        // H-4: pass UserId so service enforces ownership; returns null if not owner
        var result = await svc.GetResultAsync(UserId, jobId, ct);
        return result is null ? Accepted(new { status = "pending" }) : Ok(result);
    }

    // M-2: null-safe; throws UnauthorizedAccessException → 401 via ExceptionMiddleware
    private Guid UserId
    {
        get
        {
            var val = User.FindFirst("sub")?.Value
                   ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(val, out var id))
                throw new UnauthorizedAccessException("Invalid user identity in token.");
            return id;
        }
    }

    private static readonly HashSet<string> AllowedLanguages = ["csharp", "python", "javascript", "c", "cpp"];
}