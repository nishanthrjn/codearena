// Placeholder for SnippetServiceTests.cs
using CodeArena.Api.Data;
using CodeArena.Api.DTOs;
using CodeArena.Api.Models;
using CodeArena.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CodeArena.Api.Tests.Services;

public class SnippetServiceTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(opts);
        return db;
    }

    [Fact]
    public async Task CreateAsync_PersistsSnippetWithSlug()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, GitHubId = 1, Login = "test" });
        await db.SaveChangesAsync();

        var svc = new SnippetService(db, NullLogger<SnippetService>.Instance);
        var request = new CreateSnippetRequest("Two Sum", "python", "print(1)", "arrays", []);
        var result = await svc.CreateAsync(userId, request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("two-sum", result.Slug);
        Assert.Single(await db.Snippets.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_GeneratesUniqueSlugOnCollision()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, GitHubId = 2, Login = "test2" });
        await db.SaveChangesAsync();

        var svc = new SnippetService(db, NullLogger<SnippetService>.Instance);
        var req = new CreateSnippetRequest("Two Sum", "python", "print(1)", "", []);

        var a = await svc.CreateAsync(userId, req);
        var b = await svc.CreateAsync(userId, req);

        Assert.Equal("two-sum", a.Slug);
        Assert.Equal("two-sum-1", b.Slug);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseForWrongUser()
    {
        using var db = CreateDb();
        var owner = Guid.NewGuid();
        var intruder = Guid.NewGuid();
        db.Users.Add(new User { Id = owner, GitHubId = 3, Login = "owner" });
        await db.SaveChangesAsync();

        var svc = new SnippetService(db, NullLogger<SnippetService>.Instance);
        var snippet = await svc.CreateAsync(owner, new("Test", "python", "x=1", "", []));

        var deleted = await svc.DeleteAsync(intruder, snippet.Id);
        Assert.False(deleted);
    }
}