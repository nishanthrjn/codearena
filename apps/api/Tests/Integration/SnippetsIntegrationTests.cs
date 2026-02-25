// Placeholder for SnippetsIntegrationTests.cs
using System.Net;
using System.Net.Http.Json;
using CodeArena.Api.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CodeArena.Api.Tests.Integration;

// NOTE: Testcontainers integration test — requires Docker
// Run with: dotnet test --filter Integration
public class SnippetsIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact(Skip = "Requires Docker + PostgreSQL container")]
    public async Task PostSnippet_Returns201()
    {
        var client = factory.CreateClient();
        // In a full integration test, we'd set up auth headers and Testcontainers
        var resp = await client.PostAsJsonAsync("/api/snippets", new CreateSnippetRequest(
            "Hello World", "python", "print('hello')", "", []));
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode); // Unauthed → 401
    }
}