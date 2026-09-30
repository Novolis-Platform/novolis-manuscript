using System.Diagnostics.CodeAnalysis;
using Novolis.IO.Git;
using Novolis.IO.GitHub;
using Novolis.IO.Recovery;

namespace Novolis.Manuscript.IO;

/// <summary>Local git façade for manuscript content roots.</summary>
[ExcludeFromCodeCoverage(Justification = "Thin wrapper over GitRepositoryService (requires a real git repo).")]
public sealed class ManuscriptGitWorkspace
{
    readonly GitRepositoryService _git;

    /// <summary>Creates a façade around a <see cref="GitRepositoryService"/>.</summary>
    public ManuscriptGitWorkspace(GitRepositoryService git) =>
        _git = git ?? throw new ArgumentNullException(nameof(git));

    /// <summary>Creates a checkpoint commit for the content root.</summary>
    public GitOperationResult Checkpoint(string contentRoot, string message, CheckpointOptions? options = null) =>
        _git.Checkpoint(contentRoot, message, options);
}
