using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>On-disk audiobook manifest (<c>manifest.json</c>).</summary>
[ExcludeFromCodeCoverage(Justification = "Audiobook artifact I/O orthogonal to print remodel.")]
public sealed class AudiobookManifest
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Book identifier.</summary>
    public required string BookId { get; init; }

    /// <summary>Chapter entries in playback order.</summary>
    public required IReadOnlyList<AudiobookManifestChapter> Chapters { get; init; }

    /// <summary>Relative path to concatenated MP3 when present.</summary>
    public string? ConcatenatedMp3Path { get; init; }

    /// <summary>Relative path to M4B when present.</summary>
    public string? M4bPath { get; init; }

    /// <summary>Writes the manifest to disk.</summary>
    public void Save(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }

    /// <summary>Reads a manifest from disk.</summary>
    public static AudiobookManifest Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return JsonSerializer.Deserialize<AudiobookManifest>(File.ReadAllText(path), JsonOptions)
               ?? throw new InvalidDataException($"Manifest is empty: {path}");
    }
}
