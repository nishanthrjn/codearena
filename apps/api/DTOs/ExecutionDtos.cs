// Placeholder for ExecutionDtos.cs
namespace CodeArena.Api.DTOs;

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