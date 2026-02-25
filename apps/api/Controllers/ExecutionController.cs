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
    public async Task<IActionResult> Run([FromBody] RunRequest req, CancellationToken ct)
    {
        if (!AllowedLanguages.Contains(req.Language)) return BadRequest("Unsupported language");
        if (req.Code.Length > 100_000) return BadRequest("Code too large");
        if (req.StdIn.Length > 32_768) return BadRequest("Stdin too large");
        var jobId = await svc.EnqueueRunAsync(UserId, req, ct);
        return Accepted(new JobSubmittedDto(jobId, "queued"));
    }

    [HttpPost("test")]
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
        var result = await svc.GetResultAsync(jobId, ct);
        return result is null ? Accepted(new { status = "pending" }) : Ok(result);
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    private static readonly HashSet<string> AllowedLanguages = ["csharp", "python", "javascript", "c", "cpp"];
}