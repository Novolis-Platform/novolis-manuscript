using System.Diagnostics.CodeAnalysis;
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
