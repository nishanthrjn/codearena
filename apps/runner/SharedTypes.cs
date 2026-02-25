// Transport types shared between the API (producer) and Runner (consumer) via the Redis job queue.
// Defined here so the Runner project is self-contained with no API project reference.
// The JSON property names must match what ExecutionService serializes on the API side.
namespace CodeArena.Runner;

/// <summary>Job payload pushed to the Redis queue by the API.</summary>
public class ExecutionJob
{
    public Guid     Id            { get; set; } = Guid.NewGuid();
    public Guid     UserId        { get; set; }
    public string   Language      { get; set; } = "";
    public string   Code          { get; set; } = "";
    public string   StdIn         { get; set; } = "";
    public string   JobType       { get; set; } = "run";   // "run" | "test"
    public string   Status        { get; set; } = "queued";
    public string?  Output        { get; set; }
    public int?     ExitCode      { get; set; }
    public long     DurationMs    { get; set; }
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
    public string   TestCasesJson { get; set; } = "[]";
}

/// <summary>A single test-case input used when JobType == "test".</summary>
public record TestCaseDto(
    Guid?  Id,
    string Name,
    string StdIn,
    string Expected,
    int    OrderIndex
);

/// <summary>The outcome of one test case.</summary>
public record TestCaseResultDto(
    string Name,
    bool   Passed,
    string Stdout,
    string Stderr,
    string Expected,
    long   DurationMs
);

/// <summary>Final result written to Redis and forwarded to the API hub.</summary>
public record ExecutionResultDto(
    Guid                     JobId,
    string                   Status,
    string?                  Stdout,
    string?                  Stderr,
    int?                     ExitCode,
    long                     DurationMs,
    List<TestCaseResultDto>? TestResults
);
