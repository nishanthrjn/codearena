// Placeholder for GitHubController.cs
using CodeArena.Api.DTOs;
using CodeArena.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeArena.Api.Controllers;

[ApiController]
[Route("api/github")]
[Authorize]
public class GitHubController(IGitHubService gh) : ControllerBase
{
    [HttpGet("repos")]
    public async Task<IActionResult> ListRepos(CancellationToken ct)
        => Ok(await gh.ListReposAsync(UserId, ct));

    [HttpPost("push")]
    public async Task<IActionResult> Push([FromBody] PushToGitHubRequest req, CancellationToken ct)
    {
        var sha = await gh.PushSnippetAsync(UserId, req, ct);
        return Ok(new { commitSha = sha });
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}