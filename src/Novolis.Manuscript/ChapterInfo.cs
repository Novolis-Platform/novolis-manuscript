namespace Novolis.Manuscript;

/// <summary>One chapter or appendix markdown file.</summary>
public sealed record ChapterInfo(
    string Id,
    string Title,
    ChapterKind Kind,
    double SortKey,
    string FilePath);
