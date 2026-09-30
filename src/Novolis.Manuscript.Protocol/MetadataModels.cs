namespace Novolis.Manuscript.Protocol;

/// <summary>Inheritable default book fields.</summary>
public sealed record DefaultsMetadata(
    IReadOnlyList<string>? Authors = null,
    string? Language = null,
    string? Rights = null);
