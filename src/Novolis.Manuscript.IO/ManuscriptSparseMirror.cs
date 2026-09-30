using System.Diagnostics.CodeAnalysis;
using Novolis.IO.Git;
using Novolis.IO.GitHub;
using Novolis.IO.Recovery;

namespace Novolis.Manuscript.IO;

/// <summary>Sparse GitHub mirror façade for manuscript content prefixes.</summary>
[ExcludeFromCodeCoverage(Justification = "Thin wrapper over SparseRepoMirror (network/git).")]
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
