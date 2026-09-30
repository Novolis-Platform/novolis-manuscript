using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>One chapter entry in <see cref="AudiobookManifest"/>.</summary>
public sealed class AudiobookManifestChapter
{
    /// <summary>Chapter id.</summary>
    public required string Id { get; init; }

    /// <summary>Chapter title.</summary>
    public required string Title { get; init; }

    /// <summary>Speech plan hash used for cache validation.</summary>
    public required string PlanHash { get; init; }

    /// <summary>Relative path to chapter MP3 from manifest directory.</summary>
    public required string Mp3Path { get; init; }

    /// <summary>Approximate chapter duration in milliseconds.</summary>
    public long DurationMs { get; init; }
}
