namespace Novolis.Manuscript.Protocol;

/// <summary>Inheritable default book fields.</summary>
public sealed record DefaultsMetadata(
    IReadOnlyList<string>? Authors = null,
    string? Language = null,
    string? Rights = null);

/// <summary>Workspace root metadata from <c>manuscript.yaml</c>.</summary>
public sealed record WorkspaceMetadata(
    string Protocol,
    int Version,
    DefaultsMetadata? Defaults = null,
    IReadOnlyDictionary<string, object?>? Extensions = null);

/// <summary>Universe metadata from <c>universe.yaml</c>.</summary>
public sealed record UniverseMetadata(
    string Title,
    string? Description = null,
    DefaultsMetadata? Defaults = null,
    IReadOnlyDictionary<string, object?>? Extensions = null);

/// <summary>Subject metadata from <c>subject.yaml</c>.</summary>
public sealed record SubjectMetadata(
    string Title,
    string? Description = null,
    DefaultsMetadata? Defaults = null,
    IReadOnlyDictionary<string, object?>? Extensions = null);

/// <summary>Series metadata from <c>series.yaml</c>.</summary>
public sealed record SeriesMetadata(
    string Title,
    string? Description = null,
    DefaultsMetadata? Defaults = null,
    IReadOnlyDictionary<string, object?>? Extensions = null);

/// <summary>Word-count authoring targets.</summary>
public sealed record TargetsMetadata(int? Words = null);

/// <summary>Publication metadata.</summary>
public sealed record PublicationMetadata(
    string? Version = null,
    string? Isbn = null,
    string? Date = null);

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

/// <summary>Optional reference front matter.</summary>
public sealed record ReferenceMetadata(
    IReadOnlyList<string>? Aliases = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyDictionary<string, object?>? Extensions = null);
