// Placeholder for GitHubDtos.cs
using System.ComponentModel.DataAnnotations;

namespace CodeArena.Api.DTOs;

public record PushToGitHubRequest(
    Guid SnippetId,
    [Required, StringLength(200), RegularExpression(@"^[a-zA-Z0-9\-_.]+/[a-zA-Z0-9\-_.]+$",
        ErrorMessage = "RepoFullName must be in 'owner/repo' format with alphanumeric, hyphen, dot, or underscore characters only.")]
    string RepoFullName,   // "owner/repo"
    [Required, StringLength(250), RegularExpression(@"^[a-zA-Z0-9._\-/]{1,250}$",
        ErrorMessage = "Branch name contains invalid characters.")]
    string Branch,
    [Required, StringLength(500)]
    string CommitMessage
);

public record GitHubRepoDto(
    string FullName,
    string DefaultBranch,
    bool Private
);