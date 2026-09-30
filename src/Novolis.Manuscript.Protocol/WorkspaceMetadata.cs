namespace Novolis.Manuscript.Protocol;

/// <summary>Workspace root metadata from <c>manuscript.yaml</c>.</summary>
public sealed record WorkspaceMetadata(
    string Protocol,
    int Version,
    DefaultsMetadata? Defaults = null,
    IReadOnlyDictionary<string, object?>? Extensions = null);
