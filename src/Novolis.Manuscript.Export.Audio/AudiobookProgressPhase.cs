using System.Diagnostics.CodeAnalysis;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>High-level phase of audiobook generation.</summary>
public enum AudiobookProgressPhase
{
    /// <summary>Per-chapter TTS synthesis (or cache hit).</summary>
    Synthesizing,

    /// <summary>Concatenating chapter MP3s into a book MP3.</summary>
    AssemblingMp3,

    /// <summary>Encoding chapter MP3s into an M4B.</summary>
    AssemblingM4b,

    /// <summary>Writing manifest.json.</summary>
    WritingManifest,

    /// <summary>All work finished.</summary>
    Completed,
}
