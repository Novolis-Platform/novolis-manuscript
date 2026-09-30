namespace Novolis.Manuscript.Protocol;

/// <summary>Chapter or appendix YAML front matter.</summary>
public sealed record ChapterMetadata(
    string? Status = null,
    IReadOnlyList<string>? Tags = null,
    string? Date = null,
    string? Time = null,
    string? System = null,
    IReadOnlyList<string>? Locations = null,
    string? Pov = null,
    IReadOnlyList<string>? Characters = null,
    IReadOnlyDictionary<string, object?>? Extensions = null);
