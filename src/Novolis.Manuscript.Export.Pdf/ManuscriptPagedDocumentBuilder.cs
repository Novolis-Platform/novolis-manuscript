using Novolis.Documents;
using Novolis.Documents.Skia;
using Novolis.Markup.Markdown.Documents;
using Novolis.Math.Measure;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>
/// Bridges assembled manuscript Markdown + <see cref="ManuscriptPrintSettings"/> into a
/// <see cref="PagedDocument"/> via <see cref="MarkdownPagedDocumentMapper"/>, and writes PDF via
/// <c>Novolis.Documents.Skia</c> (no QuestPDF, no Markdig).
/// </summary>
internal static class ManuscriptPagedDocumentBuilder
{
    /// <summary>Cover metadata for a book PDF.</summary>
    internal readonly record struct BookCoverMeta(
        string Title,
        string? Subtitle,
        string? Series,
        string? Author,
        string? Rights);

    /// <summary>Writes a book PDF from already-assembled reader/author Markdown.</summary>
    public static void WriteBookPdf(
        string markdown,
        string pdfPath,
        BookCoverMeta cover,
        ManuscriptPrintSettings settings)
    {
        var options = ToOptions(
            settings,
            cover.Title,
            cover.Subtitle,
            cover.Author,
            cover.Series,
            cover.Rights,
            includeToc: false);
        var document = MarkdownPagedDocumentMapper.FromMarkdown(markdown, options);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pdfPath))!);
        DocumentPdf.Write(document, pdfPath);
    }

    /// <summary>Writes a reference-manual PDF. <paramref name="markdown"/> should already include a leading Contents section.</summary>
    public static void WriteReferencePdf(
        string coverTitle,
        string? coverSubtitle,
        string markdown,
        string pdfPath,
        ManuscriptPrintSettings settings)
    {
        var options = ToOptions(settings, coverTitle, coverSubtitle, author: null, series: null, rights: null, includeToc: false);
        var document = MarkdownPagedDocumentMapper.FromMarkdown(markdown, options);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pdfPath))!);
        DocumentPdf.Write(document, pdfPath);
    }

    static MarkdownPagedExportOptions ToOptions(
        ManuscriptPrintSettings settings,
        string title,
        string? subtitle,
        string? author,
        string? series,
        string? rights,
        bool includeToc) =>
        new()
        {
            Title = title,
            Subtitle = subtitle,
            Author = author,
            Series = series,
            Rights = rights,
            IncludeCover = settings.IncludeCover,
            IncludeToc = includeToc,
            Trim = new Size(
                LengthUnits.FromInches(settings.PageWidthInches),
                LengthUnits.FromInches(settings.PageHeightInches)),
            Margin = new Thickness(
                LengthUnits.FromInches(settings.MarginHorizontalInches),
                LengthUnits.FromInches(settings.MarginVerticalInches),
                LengthUnits.FromInches(settings.MarginRightInches),
                LengthUnits.FromInches(settings.MarginVerticalInches)),
            Typography = new Typography
            {
                BodyFontFamily = settings.BodyFontFamily,
                BodyFontSizePt = settings.BodyFontSize,
                H1SizePt = settings.ChapterTitleSizePt,
                H2SizePt = settings.H2SizePt,
                H3SizePt = settings.H3SizePt,
                SceneBreakSizePt = settings.SceneBreakSizePt,
                LineHeight = settings.LineHeight,
                ParagraphSpacingPt = settings.ParagraphSpacingPt,
            },
            HeaderTemplate = string.Empty,
            FooterTemplate = "{page} / {pages}",
            FooterOnFirstPage = false,
            FooterOnToc = true,
            FooterOnLastPage = true,
        };
}
