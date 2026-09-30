namespace Novolis.Manuscript.Protocol;

/// <summary>Series metadata from <c>series.yaml</c>.</summary>
public sealed record SeriesMetadata(
    string Title,
    string? Description = null,
    DefaultsMetadata? Defaults = null,
    IReadOnlyDictionary<string, object?>? Extensions = null);
