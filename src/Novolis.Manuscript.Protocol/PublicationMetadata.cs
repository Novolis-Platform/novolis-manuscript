namespace Novolis.Manuscript.Protocol;

/// <summary>Publication metadata.</summary>
public sealed record PublicationMetadata(
    string? Version = null,
    string? Isbn = null,
    string? Date = null);
