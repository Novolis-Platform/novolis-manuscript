using System.Diagnostics.CodeAnalysis;
using System.Text;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Paths written by a reference-manual print export.</summary>
/// <param name="MarkdownPath">Combined reference Markdown (includes TOC).</param>
/// <param name="HtmlPath">HTML companion.</param>
/// <param name="TextPath">Plain-text companion.</param>
/// <param name="PdfPath">PDF output with cover and Contents section.</param>
public sealed record ReferencePrintPaths(
    string MarkdownPath,
    string HtmlPath,
    string TextPath,
    string PdfPath);
