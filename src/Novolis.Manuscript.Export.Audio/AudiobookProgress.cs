using System.Diagnostics.CodeAnalysis;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>Snapshot of audiobook generation progress for UI.</summary>
public sealed class AudiobookProgress
{
    /// <summary>Current high-level phase.</summary>
    public AudiobookProgressPhase Phase { get; init; }

    /// <summary>Chapters finished (cached or synthesized).</summary>
    public int CompletedChapters { get; init; }

    /// <summary>Total chapters in this run.</summary>
    public int TotalChapters { get; init; }

    /// <summary>0–1 overall job progress (synthesis + assemble).</summary>
    public double OverallFraction { get; init; }

    /// <summary>Human-readable summary line.</summary>
    public required string Message { get; init; }

    /// <summary>Per-chapter rows (stable order).</summary>
    public IReadOnlyList<AudiobookChapterProgress> Chapters { get; init; } = [];
}
