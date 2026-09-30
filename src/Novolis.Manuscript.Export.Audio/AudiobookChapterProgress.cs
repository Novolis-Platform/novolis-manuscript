using System.Diagnostics.CodeAnalysis;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>Progress for one chapter inside an audiobook job.</summary>
[ExcludeFromCodeCoverage(Justification = "Audiobook progress DTO orthogonal to print remodel.")]
public sealed class AudiobookChapterProgress
{
    /// <summary>Stable chapter id.</summary>
    public required string ChapterId { get; init; }

    /// <summary>Display title.</summary>
    public required string Title { get; init; }

    /// <summary>Lifecycle state.</summary>
    public AudiobookChapterState State { get; init; }

    /// <summary>Segments completed (text + pause).</summary>
    public int CompletedSegments { get; init; }

    /// <summary>Total segments in the speech plan (0 until planned).</summary>
    public int TotalSegments { get; init; }

    /// <summary>0–1 progress within this chapter.</summary>
    public double Fraction { get; init; }

    /// <summary>Short status for UI (e. for example <c>3/12</c>, <c>cached</c>).</summary>
    public string StatusLabel =>
        State switch
        {
            AudiobookChapterState.Pending => "pending",
            AudiobookChapterState.Running when TotalSegments > 0 =>
                $"{CompletedSegments}/{TotalSegments}",
            AudiobookChapterState.Running => "starting…",
            AudiobookChapterState.Cached => "cached",
            AudiobookChapterState.Completed => "done",
            AudiobookChapterState.Failed => "failed",
            _ => State.ToString(),
        };
}
