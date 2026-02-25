// Placeholder for Snippet.cs
namespace CodeArena.Api.Models;

public class Snippet
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public Guid     UserId      { get; set; }
    public User     User        { get; set; } = null!;
    public string   Title       { get; set; } = "";
    public string   Language    { get; set; } = "";   // "csharp" | "python" | "c" | "cpp" | "javascript"
    public string   Code        { get; set; } = "";
    public string   Tags        { get; set; } = "";   // comma-separated
    public string   Slug        { get; set; } = "";
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt   { get; set; } = DateTime.UtcNow;
    public ICollection<TestCase> TestCases { get; set; } = [];
}

public class TestCase
{
    public Guid    Id         { get; set; } = Guid.NewGuid();
    public Guid    SnippetId  { get; set; }
    public Snippet Snippet    { get; set; } = null!;
    public string  Name       { get; set; } = "";
    public string  StdIn      { get; set; } = "";
    public string  Expected   { get; set; } = "";
    public int     OrderIndex { get; set; }
}

public class User
{
    public Guid   Id              { get; set; } = Guid.NewGuid();
    public long   GitHubId        { get; set; }
    public string Login           { get; set; } = "";
    public string Email           { get; set; } = "";
    public string AvatarUrl       { get; set; } = "";
    public string EncryptedToken  { get; set; } = "";   // GitHub OAuth token, encrypted
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
    public ICollection<Snippet> Snippets { get; set; } = [];
}

public class ExecutionJob
{
    public Guid     Id         { get; set; } = Guid.NewGuid();
    public Guid     UserId     { get; set; }
    public string   Language   { get; set; } = "";
    public string   Code       { get; set; } = "";
    public string   StdIn      { get; set; } = "";
    public string   JobType    { get; set; } = "run";   // "run" | "test"
    public string   Status     { get; set; } = "queued"; // queued | running | done | error
    public string?  Output     { get; set; }
    public int?     ExitCode   { get; set; }
    public long     DurationMs { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    // For test jobs, JSON array of TestCase inputs stored here
    public string   TestCasesJson { get; set; } = "[]";
}