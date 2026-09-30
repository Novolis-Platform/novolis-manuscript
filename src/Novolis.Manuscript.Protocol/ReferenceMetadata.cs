namespace Novolis.Manuscript.Protocol;

/// <summary>Optional reference front matter.</summary>
public sealed record ReferenceMetadata(
    IReadOnlyList<string>? Aliases = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyDictionary<string, object?>? Extensions = null);
