namespace Novolis.Manuscript;

/// <summary>Kind of chapter-like document in a book.</summary>
public enum ChapterKind
{
    /// <summary>Main narrative chapter.</summary>
    Chapter,

    /// <summary>Appendix document.</summary>
    Appendix,
}

/// <summary>A series under <c>content/series/{id}</c>.</summary>
public sealed record SeriesInfo(
    string Id,
    string Title,
    string DirectoryPath,
    IReadOnlyList<BookInfo> Books,
    IReadOnlyList<ReferenceSetInfo> References);

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

/// <summary>A named set of reference markdown files.</summary>
public sealed record ReferenceSetInfo(
    string Id,
    string Title,
    string DirectoryPath,
    IReadOnlyList<ReferenceFileInfo> Files);

/// <summary>One reference markdown file.</summary>
public sealed record ReferenceFileInfo(string Id, string Title, string FilePath);

/// <summary>One chapter or appendix markdown file.</summary>
public sealed record ChapterInfo(
    string Id,
    string Title,
    ChapterKind Kind,
    double SortKey,
    string FilePath);
