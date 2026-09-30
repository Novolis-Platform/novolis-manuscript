namespace Novolis.Manuscript;

/// <summary>A series under <c>content/series/{id}</c>.</summary>
public sealed record SeriesInfo(
    string Id,
    string Title,
    string DirectoryPath,
    IReadOnlyList<BookInfo> Books,
    IReadOnlyList<ReferenceSetInfo> References);
