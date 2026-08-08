using Markdig;
using Markdig.Syntax;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>QuestPDF document builders for books and reference manuals.</summary>
internal static class QuestPdfDocuments
{
    static QuestPdfDocuments()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    internal static void EnsureLicense()
    {
        // Static ctor runs on first use.
        _ = typeof(QuestPdfDocuments);
    }

    internal readonly record struct BookCoverMeta(
        string Title,
        string? Subtitle,
        string? Series,
        string? Author,
        string? Rights);

    internal readonly record struct TocEntry(int Level, string Title);

    public static void WriteBookPdf(
        string markdown,
        string pdfPath,
        BookCoverMeta cover,
        bool showAllTags,
        ManuscriptPrintSettings settings)
    {
        EnsureLicense();
        var doc = Markdown.Parse(markdown, MarkdownRenderPipeline.Instance);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pdfPath))!);

        Document.Create(c =>
        {
            if (settings.IncludeCover)
            {
                c.Page(page =>
                {
                    ApplyPageChrome(page, settings, coverMargins: true, showFooter: false);
                    // AlignMiddle/AlignCenter — not ExtendVertical spacers (those leave the title high).
                    page.Content().AlignCenter().AlignMiddle().Column(inner =>
                    {
                        inner.Item().AlignCenter().Text(cover.Title)
                            .FontSize(22).FontFamily(settings.BodyFontFamily).SemiBold();
                        if (!string.IsNullOrWhiteSpace(cover.Subtitle))
                            inner.Item().AlignCenter().PaddingTop(6).Text(cover.Subtitle).FontSize(13)
                                .FontFamily(settings.BodyFontFamily).FontColor(Colors.Grey.Darken2);
                        if (!string.IsNullOrWhiteSpace(cover.Series))
                            inner.Item().AlignCenter().PaddingTop(10).Text(cover.Series).FontSize(12)
                                .FontFamily(settings.BodyFontFamily).FontColor(Colors.Grey.Darken2);
                        if (!string.IsNullOrWhiteSpace(cover.Author))
                            inner.Item().AlignCenter().PaddingTop(18).Text(cover.Author).FontSize(11)
                                .FontFamily(settings.BodyFontFamily);
                        if (!string.IsNullOrWhiteSpace(cover.Rights))
                            inner.Item().AlignCenter().PaddingTop(28).Text(cover.Rights).FontSize(8.5f)
                                .FontFamily(settings.BodyFontFamily).FontColor(Colors.Grey.Medium);
                    });
                });
            }

            c.Page(page =>
            {
                ApplyPageChrome(page, settings, coverMargins: false);
                page.Content().Column(col =>
                {
                    col.Spacing(settings.ParagraphSpacingPt);
                    var contentColumnHasMaterial = false;
                    foreach (var block in doc)
                    {
                        if (block is HeadingBlock hb
                            && ChapterPageBreaks.ShouldBreakBeforeHeading(contentColumnHasMaterial, hb.Level))
                            col.Item().PageBreak();

                        if (QuestPdfBlockRenderer.AppendBlock(col, block, showAllTags, settings))
                            contentColumnHasMaterial = true;
                    }
                });
            });
        }).GeneratePdf(pdfPath);
    }

    public static void WriteReferencePdf(
        string coverTitle,
        string? coverSubtitle,
        IReadOnlyList<TocEntry> toc,
        string markdownBody,
        string pdfPath,
        ManuscriptPrintSettings settings)
    {
        EnsureLicense();
        var doc = Markdown.Parse(markdownBody, MarkdownRenderPipeline.Instance);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pdfPath))!);

        Document.Create(c =>
        {
            if (settings.IncludeCover)
            {
                c.Page(page =>
                {
                    ApplyPageChrome(page, settings, coverMargins: true, showFooter: false);
                    page.Content().AlignCenter().AlignMiddle().Column(inner =>
                    {
                        inner.Item().AlignCenter().Text(coverTitle)
                            .FontSize(22).FontFamily(settings.BodyFontFamily).SemiBold();
                        if (!string.IsNullOrWhiteSpace(coverSubtitle))
                            inner.Item().AlignCenter().PaddingTop(6).Text(coverSubtitle).FontSize(13)
                                .FontFamily(settings.BodyFontFamily).FontColor(Colors.Grey.Darken2);
                        inner.Item().AlignCenter().PaddingTop(28)
                            .Text($"Generated {DateTime.UtcNow:yyyy-MM-dd} UTC").FontSize(9)
                            .FontFamily(settings.BodyFontFamily).FontColor(Colors.Grey.Medium);
                    });
                });
            }

            c.Page(page =>
            {
                ApplyPageChrome(page, settings, coverMargins: false);
                page.Content().Column(col =>
                {
                    col.Spacing(settings.ParagraphSpacingPt);
                    if (toc.Count > 0)
                    {
                        col.Item().Text("Contents").FontSize(16).FontFamily(settings.BodyFontFamily).SemiBold();
                        foreach (var e in toc)
                        {
                            var pad = Math.Max(0f, (e.Level - 1) * 14f);
                            col.Item().PaddingLeft(pad).Text(t =>
                            {
                                t.DefaultTextStyle(s => s.FontSize(10f).FontFamily(settings.BodyFontFamily).LineHeight(1.35f));
                                t.Span(e.Title);
                            });
                        }

                        col.Item().PageBreak();
                    }

                    var contentColumnHasMaterial = toc.Count > 0;
                    foreach (var block in doc)
                    {
                        if (block is HeadingBlock hb
                            && ChapterPageBreaks.ShouldBreakBeforeHeading(contentColumnHasMaterial, hb.Level))
                            col.Item().PageBreak();

                        if (QuestPdfBlockRenderer.AppendBlock(col, block, showAllTags: false, settings))
                            contentColumnHasMaterial = true;
                    }
                });
            });
        }).GeneratePdf(pdfPath);
    }

    static void ApplyPageChrome(
        PageDescriptor page,
        ManuscriptPrintSettings settings,
        bool coverMargins,
        bool showFooter = true)
    {
        page.Size(settings.PageWidthInches, settings.PageHeightInches, Unit.Inch);
        if (coverMargins)
        {
            // Symmetric margins so AlignMiddle is optically centered on the page.
            page.MarginVertical(settings.MarginVerticalInches, Unit.Inch);
            page.MarginHorizontal(settings.MarginHorizontalInches, Unit.Inch);
        }
        else
        {
            page.MarginTop(settings.MarginVerticalInches, Unit.Inch);
            page.MarginBottom(settings.MarginVerticalInches, Unit.Inch);
            page.MarginLeft(settings.MarginHorizontalInches, Unit.Inch);
            page.MarginRight(settings.MarginRightInches, Unit.Inch);
        }

        if (!showFooter)
            return;

        page.Footer()
            .AlignCenter()
            .Text(tx =>
            {
                tx.DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Medium).FontFamily(settings.BodyFontFamily));
                tx.CurrentPageNumber();
                tx.Span(" / ");
                tx.TotalPages();
            });
    }
}
