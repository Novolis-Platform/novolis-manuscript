using Novolis.Markup.Markdown;

namespace Novolis.Manuscript;

/// <summary>One ordered chapter or appendix: metadata plus a parsed markdown body.</summary>
public sealed record ChapterDocument(
    int Order,
    string Slug,
    string Title,
    ChapterKind Kind,
    string FilePath,
    IMarkdownDocument Body);
