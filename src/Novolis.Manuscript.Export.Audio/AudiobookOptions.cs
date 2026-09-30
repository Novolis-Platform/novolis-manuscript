namespace Novolis.Manuscript.Export.Audio;

/// <summary>Options for <see cref="AudiobookPipeline"/>.</summary>
public sealed class AudiobookOptions
{
    /// <summary>Root output directory for chapters, manifest, and assembled files.</summary>
    public required string OutputDirectory { get; init; }

    /// <summary>Rebuild chapter MP3s even when cache hash matches.</summary>
    public bool Force { get; init; }

    /// <summary>Maximum parallel chapter synthesis jobs.</summary>
    public int ParallelJobs { get; init; } = 1;

    /// <summary>When set, only chapters whose ids are in this set are synthesized.</summary>
    public IReadOnlySet<string>? ChapterFilter { get; init; }

    /// <summary>Post-synthesis assembly mode.</summary>
    public AudiobookAssembleMode AssembleMode { get; init; } = AudiobookAssembleMode.ConcatMp3;

    /// <summary>Silence gap inserted between chapters during assembly (ms).</summary>
    public int ChapterGapMs { get; init; } = 1000;
}
