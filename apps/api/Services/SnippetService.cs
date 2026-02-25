// Placeholder for SnippetService.cs
using CodeArena.Api.Data;
using CodeArena.Api.DTOs;
using CodeArena.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeArena.Api.Services;

public interface ISnippetService
{
    Task<List<SnippetSummaryDto>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<SnippetDetailDto?> GetAsync(Guid userId, Guid snippetId, CancellationToken ct = default);
    Task<SnippetDetailDto> CreateAsync(Guid userId, CreateSnippetRequest req, CancellationToken ct = default);
    Task<SnippetDetailDto?> UpdateAsync(Guid userId, Guid snippetId, UpdateSnippetRequest req, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, Guid snippetId, CancellationToken ct = default);
}

public class SnippetService(AppDbContext db, ILogger<SnippetService> log) : ISnippetService
{
    public async Task<List<SnippetSummaryDto>> ListAsync(Guid userId, CancellationToken ct = default)
        => await db.Snippets
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new SnippetSummaryDto(s.Id, s.Title, s.Language, s.Tags, s.Slug, s.UpdatedAt))
            .ToListAsync(ct);

    public async Task<SnippetDetailDto?> GetAsync(Guid userId, Guid snippetId, CancellationToken ct = default)
    {
        var s = await db.Snippets
            .Include(x => x.TestCases.OrderBy(t => t.OrderIndex))
            .FirstOrDefaultAsync(x => x.Id == snippetId && x.UserId == userId, ct);
        return s is null ? null : Map(s);
    }

    public async Task<SnippetDetailDto> CreateAsync(Guid userId, CreateSnippetRequest req, CancellationToken ct = default)
    {
        var slug = GenerateSlug(req.Title);
        var snippet = new Snippet
        {
            UserId = userId,
            Title = req.Title,
            Language = req.Language,
            Code = req.Code,
            Tags = req.Tags,
            Slug = await UniqueSlugAsync(userId, slug, ct)
        };
        snippet.TestCases = req.TestCases.Select((t, i) => new TestCase
        {
            Name = t.Name,
            StdIn = t.StdIn,
            Expected = t.Expected,
            OrderIndex = i
        }).ToList();
        db.Snippets.Add(snippet);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Created snippet {Id} for user {User}", snippet.Id, userId);
        return Map(snippet);
    }

    public async Task<SnippetDetailDto?> UpdateAsync(Guid userId, Guid snippetId, UpdateSnippetRequest req, CancellationToken ct = default)
    {
        var snippet = await db.Snippets
            .Include(x => x.TestCases)
            .FirstOrDefaultAsync(x => x.Id == snippetId && x.UserId == userId, ct);
        if (snippet is null) return null;

        snippet.Title = req.Title;
        snippet.Language = req.Language;
        snippet.Code = req.Code;
        snippet.Tags = req.Tags;
        snippet.UpdatedAt = DateTime.UtcNow;

        db.TestCases.RemoveRange(snippet.TestCases);
        snippet.TestCases = req.TestCases.Select((t, i) => new TestCase
        {
            SnippetId = snippet.Id,
            Name = t.Name,
            StdIn = t.StdIn,
            Expected = t.Expected,
            OrderIndex = i
        }).ToList();
        await db.SaveChangesAsync(ct);
        return Map(snippet);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid snippetId, CancellationToken ct = default)
    {
        var snippet = await db.Snippets.FirstOrDefaultAsync(
            x => x.Id == snippetId && x.UserId == userId, ct);
        if (snippet is null) return false;
        db.Snippets.Remove(snippet);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static SnippetDetailDto Map(Snippet s) => new(
        s.Id, s.Title, s.Language, s.Code, s.Tags, s.Slug, s.UpdatedAt,
        s.TestCases.Select(t => new TestCaseDto(t.Id, t.Name, t.StdIn, t.Expected, t.OrderIndex)).ToList());

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLower()
            .Replace(" ", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate("", (a, c) => a + c);
        return slug[..Math.Min(slug.Length, 60)];
    }

    private async Task<string> UniqueSlugAsync(Guid userId, string slug, CancellationToken ct)
    {
        var candidate = slug;
        var i = 1;
        while (await db.Snippets.AnyAsync(s => s.UserId == userId && s.Slug == candidate, ct))
            candidate = $"{slug}-{i++}";
        return candidate;
    }
}