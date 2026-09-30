using Novolis.Manuscript;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Paths written by a multi-format book print export.</summary>
/// <param name="MarkdownPath">Combined chapter Markdown.</param>
/// <param name="HtmlPath">HTML companion.</param>
/// <param name="TextPath">Plain-text companion.</param>
/// <param name="PdfPath">PDF output (Novolis.Documents + Documents.Skia).</param>
public sealed record BookPrintPaths(
    string MarkdownPath,
    string HtmlPath,
    string TextPath,
    string PdfPath);
