// Placeholder for SnippetsController.cs
using CodeArena.Api.DTOs;
using CodeArena.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeArena.Api.Controllers;

[ApiController]
[Route("api/snippets")]
[Authorize]
public class SnippetsController(
    ISnippetService snippets,
    IValidator<CreateSnippetRequest> validator,
    ILogger<SnippetsController> log) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await snippets.ListAsync(UserId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var s = await snippets.GetAsync(UserId, id, ct);
        return s is null ? NotFound() : Ok(s);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSnippetRequest req, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        var result = await snippets.CreateAsync(UserId, req, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSnippetRequest req, CancellationToken ct)
    {
        var result = await snippets.UpdateAsync(UserId, id, req, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await snippets.DeleteAsync(UserId, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}