using System.Diagnostics.CodeAnalysis;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>Per-chapter synthesis state.</summary>
public enum AudiobookChapterState
{
    /// <summary>Not started.</summary>
    Pending,

    /// <summary>Actively synthesizing.</summary>
    Running,

    /// <summary>Reused from plan-hash cache.</summary>
    Cached,

    /// <summary>Finished synthesizing.</summary>
    Completed,

    /// <summary>Failed.</summary>
    Failed,
}
