// Shared execution job model used by both the API (to produce jobs)
// and the Runner (to consume and process jobs) via Redis.
namespace CodeArena.Shared.Models;

public class ExecutionJob
{
    public Guid     Id         { get; set; } = Guid.NewGuid();
    public Guid     UserId     { get; set; }
    public string   Language   { get; set; } = "";
    public string   Code       { get; set; } = "";
    public string   StdIn      { get; set; } = "";
    public string   JobType    { get; set; } = "run";   // "run" | "test"
    public string   Status     { get; set; } = "queued";
    public string?  Output     { get; set; }
    public int?     ExitCode   { get; set; }
    public long     DurationMs { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public string   TestCasesJson { get; set; } = "[]";
}
