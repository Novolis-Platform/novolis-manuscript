using Novolis.IO.Git;
using Novolis.IO.GitHub;
using Novolis.IO.Recovery;

namespace Novolis.Manuscript.IO;

/// <summary>Working-copy recovery helpers for manuscript editors.</summary>
public static class ManuscriptWorkingCopy
{
    /// <summary>Creates a recovery store under <c>{contentRoot}/.writer/recovery</c>.</summary>
    public static ContentRecoveryStore CreateRecoveryStore(string contentRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRoot);
        return new ContentRecoveryStore(Path.Combine(contentRoot, ".writer", "recovery"));
    }
}

/// <summary>Local git façade for manuscript content roots.</summary>
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

/// <summary>Sparse GitHub mirror façade for manuscript content prefixes.</summary>
public sealed class ManuscriptSparseMirror
{
    readonly SparseRepoMirror _mirror;

    /// <summary>Creates a façade around a <see cref="SparseRepoMirror"/>.</summary>
    public ManuscriptSparseMirror(SparseRepoMirror mirror) =>
        _mirror = mirror ?? throw new ArgumentNullException(nameof(mirror));

    /// <summary>Underlying mirror.</summary>
    public SparseRepoMirror Mirror => _mirror;

    /// <summary>Marks a relative path dirty.</summary>
    public void NoteDirty(string relativePath) => _mirror.NoteDirty(relativePath);

    /// <summary>Dirty path count.</summary>
    public int DirtyCount => _mirror.DirtyCount;

    /// <summary>Pulls the sparse tree.</summary>
    public Task<MirrorPullResult> PullAsync(CancellationToken cancellationToken = default) =>
        _mirror.PullAsync(cancellationToken);

    /// <summary>Saves, commits, and pushes dirty paths.</summary>
    public Task<MirrorPushResult> SaveCommitPushAsync(string? message = null, CancellationToken cancellationToken = default) =>
        _mirror.SaveCommitPushAsync(message, cancellationToken);
}
