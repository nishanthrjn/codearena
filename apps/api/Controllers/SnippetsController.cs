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
    IValidator<CreateSnippetRequest> createValidator,
    IValidator<UpdateSnippetRequest> updateValidator,  // M-6
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
        var validation = await createValidator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        var result = await snippets.CreateAsync(UserId, req, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSnippetRequest req, CancellationToken ct)
    {
        // M-6: validate update requests the same way as create
        var validation = await updateValidator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        var result = await snippets.UpdateAsync(UserId, id, req, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await snippets.DeleteAsync(UserId, id, ct);
        return deleted ? NoContent() : NotFound();
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
}