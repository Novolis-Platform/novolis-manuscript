namespace Novolis.Manuscript;

/// <summary>A book under a series or <c>content/books/{id}</c>.</summary>
public sealed record BookInfo(
    string Id,
    string Title,
    string? Subtitle,
    string? Author,
    string DirectoryPath,
    string? SeriesId,
    IReadOnlyList<ChapterInfo> Chapters,
    bool ChapterOrderFromHeading,
    bool DebugMode,
    IReadOnlyList<ReferenceSetInfo> References);
