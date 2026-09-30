namespace Novolis.Manuscript.Protocol;

/// <summary>Effective book metadata after inheritance.</summary>
public sealed record BookMetadata(
    string Title,
    string? Subtitle = null,
    int? Order = null,
    IReadOnlyList<string>? Authors = null,
    string? Language = null,
    string? Description = null,
    string? Rights = null,
    TargetsMetadata? Targets = null,
    PublicationMetadata? Publication = null,
    IReadOnlyDictionary<string, object?>? Extensions = null);
