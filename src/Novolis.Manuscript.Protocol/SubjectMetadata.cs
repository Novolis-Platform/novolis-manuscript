namespace Novolis.Manuscript.Protocol;

/// <summary>Subject metadata from <c>subject.yaml</c>.</summary>
public sealed record SubjectMetadata(
    string Title,
    string? Description = null,
    DefaultsMetadata? Defaults = null,
    IReadOnlyDictionary<string, object?>? Extensions = null);
