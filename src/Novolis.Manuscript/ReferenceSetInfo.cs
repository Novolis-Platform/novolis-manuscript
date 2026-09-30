namespace Novolis.Manuscript;

/// <summary>A named set of reference markdown files.</summary>
public sealed record ReferenceSetInfo(
    string Id,
    string Title,
    string DirectoryPath,
    IReadOnlyList<ReferenceFileInfo> Files);
