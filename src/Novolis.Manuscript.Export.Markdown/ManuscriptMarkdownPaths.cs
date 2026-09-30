namespace Novolis.Manuscript.Export.Markdown;

/// <summary>Paths written by <see cref="ManuscriptMarkdownExporter"/>.</summary>
public sealed record ManuscriptMarkdownPaths(
    string ReaderMarkdownPath,
    string? AuthorMarkdownPath,
    string? HtmlPath);
