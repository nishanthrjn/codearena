// Placeholder for SandboxOptions.cs
namespace CodeArena.Runner;

public class SandboxOptions
{
    public int TimeoutSeconds { get; set; } = 2;
    public long MemoryLimitBytes { get; set; } = 268_435_456; // 256 MB
    public long MaxOutputBytes { get; set; } = 1_048_576;   // 1 MB
    public int MaxTestCases { get; set; } = 10;
    public string WorkspaceBasePath { get; set; } = "/tmp/codearena-workspaces";
    public int CpuQuotaMicroseconds { get; set; } = 100_000; // 100ms per 100ms period = 1 CPU
    public int CpuPeriodMicroseconds { get; set; } = 100_000;
}