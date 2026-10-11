using Novolis.Documents;
using Novolis.Documents.Skia;
using Novolis.Markup.Markdown;
using Novolis.Markup.Markdown.Documents;
using Novolis.Math.Measure;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>
/// Bridges typed markdown + <see cref="ManuscriptPrintSettings"/> into a
/// <see cref="Document"/> via <see cref="MarkdownDocumentMapper"/>, and writes PDF via
/// <c>Novolis.Documents.Skia</c>.
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

    /// <summary>Writes a PDF from a typed markdown document (no string round-trip).</summary>
    public static void WriteDocument(
        IMarkdownDocument markdown,
        string pdfPath,
        BookCoverMeta cover,
        ManuscriptPrintSettings settings,
        bool includeToc,
        bool includeCover)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);
        ArgumentNullException.ThrowIfNull(settings);
        var options = ToOptions(
            settings,
            cover.Title,
            cover.Subtitle,
            cover.Author,
            cover.Series,
            cover.Rights,
            includeToc,
            includeCover);
        var document = MarkdownDocumentMapper.FromDocument(markdown, options);
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
        bool includeToc,
        bool includeCover) =>
        new()
        {
            Title = title,
            Subtitle = subtitle,
            Author = author,
            Series = series,
            Rights = rights,
            IncludeCover = includeCover,
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
                H4SizePt = settings.H4SizePt,
                CodeFontFamily = settings.CodeFontFamily,
                SceneBreakSizePt = settings.SceneBreakSizePt,
                LineHeight = settings.LineHeight,
                ParagraphSpacingPt = settings.ParagraphSpacingPt,
            },
            UseTextbookChrome = settings.UseTextbookChrome,
            EnableChapterDatelineBoxes = settings.IncludePublicDateline,
            ShowCodeLineNumbers = settings.UseTextbookChrome,
            HighlightCode = settings.UseTextbookChrome,
            HeaderTemplate = string.Empty,
            FooterTemplate = "{page} / {pages}",
            FooterOnFirstPage = false,
            FooterOnToc = true,
            FooterOnLastPage = true,
        };
}
