// Shared execution DTOs used by both the API and the Runner.
// The API enqueues jobs to Redis; the Runner dequeues and processes them.
// Both must use the same type definitions so JSON round-trips correctly.
namespace CodeArena.Shared.DTOs;

public record RunRequest(
    string Language,
    string Code,
    string StdIn = ""
);

public record TestRequest(
    string Language,
    string Code,
    List<TestCaseDto> TestCases
);

public record JobSubmittedDto(Guid JobId, string Status);

public record TestCaseDto(
    Guid? Id,
    string Name,
    string StdIn,
    string Expected,
    int OrderIndex
);

public record TestCaseResultDto(
    string Name,
    bool Passed,
    string Stdout,
    string Stderr,
    string Expected,
    long DurationMs
);

public record ExecutionResultDto(
    Guid JobId,
    string Status,
    string? Stdout,
    string? Stderr,
    int? ExitCode,
    long DurationMs,
    List<TestCaseResultDto>? TestResults
);
