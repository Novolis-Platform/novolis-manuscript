using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>Result of <see cref="AudiobookPipeline"/> generation.</summary>
public sealed class AudiobookResult
{
    /// <summary>Absolute path to <c>manifest.json</c>.</summary>
    public required string ManifestPath { get; init; }

    /// <summary>Absolute paths to chapter MP3 files.</summary>
    public required IReadOnlyList<string> ChapterPaths { get; init; }

    /// <summary>Absolute path to concatenated MP3 when produced.</summary>
    public string? ConcatenatedMp3Path { get; init; }

    /// <summary>Absolute path to M4B when produced.</summary>
    public string? M4bPath { get; init; }

    /// <summary>Loaded manifest.</summary>
    public required AudiobookManifest Manifest { get; init; }
}
