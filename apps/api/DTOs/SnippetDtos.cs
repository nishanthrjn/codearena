// Placeholder for SnippetDtos.cs
using FluentValidation;

namespace CodeArena.Api.DTOs;

public record SnippetSummaryDto(
    Guid Id,
    string Title,
    string Language,
    string Tags,
    string Slug,
    DateTime UpdatedAt
);

public record TestCaseDto(
    Guid? Id,
    string Name,
    string StdIn,
    string Expected,
    int OrderIndex
);

public record SnippetDetailDto(
    Guid Id,
    string Title,
    string Language,
    string Code,
    string Tags,
    string Slug,
    DateTime UpdatedAt,
    List<TestCaseDto> TestCases
);

public record CreateSnippetRequest(
    string Title,
    string Language,
    string Code,
    string Tags,
    List<TestCaseDto> TestCases
);

public record UpdateSnippetRequest(
    string Title,
    string Language,
    string Code,
    string Tags,
    List<TestCaseDto> TestCases
);

public class CreateSnippetValidator : AbstractValidator<CreateSnippetRequest>
{
    private static readonly HashSet<string> ValidLanguages =
        ["csharp", "python", "javascript", "c", "cpp"];

    public CreateSnippetValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Language).Must(l => ValidLanguages.Contains(l))
            .WithMessage("Unsupported language. Use: csharp, python, javascript, c, cpp");
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100_000);
        RuleFor(x => x.Tags).MaximumLength(500);
        RuleFor(x => x.TestCases).Must(tc => tc.Count <= 10)
            .WithMessage("Maximum 10 test cases allowed");
        RuleForEach(x => x.TestCases).ChildRules(tc =>
        {
            tc.RuleFor(t => t.StdIn).MaximumLength(32_768);
            tc.RuleFor(t => t.Expected).MaximumLength(1_048_576);
        });
    }
}