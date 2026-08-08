using Novolis.Manuscript;
using Novolis.Manuscript.Export.Pdf;

namespace Novolis.Manuscript.Unit.Export;

public sealed class ChapterMetadataHtmlCoverageTests
{
    [Test]
    public async Task Transform_reader_and_debug_blockquotes()
    {
        var readerMd = """
            # Title

            > [!date] 2495.001
            > [!time] 10:00
            > [!system] Sys
            > [!location] Loc
            > [!pov] James

            Body.
            """;
        var assembled = BookPrintAssembler.AssembleMarkdown(
            new BookPrintDocument(
                "b",
                new BookPrintCover("T", null, null, null, null),
                [BookPrintAssembler.FromChapterMarkdown(readerMd)],
                false),
            authorMode: true);

        var htmlPath = Path.Combine(Path.GetTempPath(), $"ms-cmh-{Guid.NewGuid():N}.html");
        try
        {
            ManuscriptDocumentEmitters.WriteHtml("T", assembled, htmlPath, null, showAllTags: true);
            var debugHtml = await File.ReadAllTextAsync(htmlPath);
            await Assert.That(debugHtml).Contains("debug-mode");
            await Assert.That(debugHtml.Contains("DATE") || debugHtml.Contains("date") || debugHtml.Contains("sl-k")).IsTrue();

            ManuscriptDocumentEmitters.WriteHtml("T", assembled, htmlPath, null, showAllTags: false);
            var readerHtml = await File.ReadAllTextAsync(htmlPath);
            await Assert.That(readerHtml).DoesNotContain("debug-mode");

            // Multi-tag single paragraph (Markdig may merge).
            var multi = "> [!date] 1 [!time] 2 [!location] L\n\n# H\n\nB\n";
            ManuscriptDocumentEmitters.WriteHtml("T", multi, htmlPath, null, showAllTags: false);
            var multiHtml = await File.ReadAllTextAsync(htmlPath);
            await Assert.That(multiHtml.Length).IsGreaterThan(20);

            var plainQuote = "> Not metadata, just a quote.\n\n# H\n\nB\n";
            ManuscriptDocumentEmitters.WriteHtml("T", plainQuote, htmlPath, null, showAllTags: false);
            await Assert.That((await File.ReadAllTextAsync(htmlPath))).Contains("blockquote");
        }
        finally
        {
            try { File.Delete(htmlPath); } catch { /* ignore */ }
        }
    }

    [Test]
    public async Task Metadata_parse_yaml_invalid_falls_back_naive()
    {
        var md = """
            ---
            date "missing colon"
            location: Place
            ---

            # H

            Body
            """;
        var (meta, body, format) = ManuscriptMetadata.Parse(md);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Yaml);
        await Assert.That(body).Contains("# H");
        _ = meta;
    }

    [Test]
    public async Task Print_settings_font_aliases()
    {
        var s = new ManuscriptPrintSettings();
        s.FontFamily = "Times New Roman";
        s.BodyFontSizePt = 12;
        await Assert.That(s.BodyFontFamily).IsEqualTo("Times New Roman");
        await Assert.That(s.BodyFontSize).IsEqualTo(12);
        var opts = s.ToPdfOptions("t", "s", "a");
        await Assert.That(opts.Title).IsEqualTo("t");
        var path = Path.Combine(Path.GetTempPath(), $"ps-{Guid.NewGuid():N}.json");
        try
        {
            s.Save(path);
            var loaded = ManuscriptPrintSettings.Load(path);
            await Assert.That(loaded.BodyFontFamily).IsEqualTo("Times New Roman");
            await Assert.That(ManuscriptPrintSettings.Load(null).BodyFontSize).IsGreaterThan(0);
            await Assert.That(ManuscriptPrintSettings.Load(Path.Combine(Path.GetTempPath(), "missing-print.json")).IncludeCover).IsTrue();
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }
}
