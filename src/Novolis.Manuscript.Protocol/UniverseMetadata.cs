namespace Novolis.Manuscript.Protocol;

/// <summary>Universe metadata from <c>universe.yaml</c>.</summary>
public sealed record UniverseMetadata(
    string Title,
    string? Description = null,
    DefaultsMetadata? Defaults = null,
    IReadOnlyDictionary<string, object?>? Extensions = null);
