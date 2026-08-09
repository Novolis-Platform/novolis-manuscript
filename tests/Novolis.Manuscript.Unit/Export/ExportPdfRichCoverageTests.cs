using Novolis.Manuscript;
using Novolis.Manuscript.Export.Pdf;

namespace Novolis.Manuscript.Unit;

public sealed class ExportPdfRichCoverageTests
{
    const string RichChapter = """
        # Chapter 1 - Opening

        > [!date] 2026-01-01 [!time] 08:00 [!pov] Ryn [!location] Bridge

        > Orbit 7 · Watch

        > **Note** keep this callout.

        First paragraph with **bold** and *italic* and `code`.

        ***

        ## Section Two

        ### Deep heading

        - unordered one
        - unordered two

        1. ordered one
        2. ordered two

        | Col A | Col B |
        | --- | --- |
        | 1 | two |
        | 3 | four |

        ```csharp
        var x = 1;
        ```

            indented code line

        Closing words.
        """;

    [Test]
    public async Task BookPrintExporter_rich_markdown_txt_html_and_options()
    {
        var root = CreateBookWorkspace(RichChapter, includeRights: true);
        var outDir = Path.Combine(Path.GetTempPath(), $"ms-pdf-out-{Guid.NewGuid():N}");
        try
        {
            var bookDir = Path.Combine(root, "content", "series", "demo", "books", "book-one");
            var settingsPath = Path.Combine(root, "print.json");
            File.WriteAllText(settingsPath, """{"includeCover":true,"bodyFontSize":11,"debugMode":false}""");

            var paths = BookPrintExporter.ExportBookFolder(
                bookDir,
                outDir,
                "demo",
                "book-one",
                new BookPrintOptions
                {
                    ShowAllMetadataTags = true,
                    SeriesTitle = "Demo Series Title",
                    Rights = "All rights reserved.",
                    PrintSettingsPath = settingsPath,
                });

            var txt = await File.ReadAllTextAsync(paths.TextPath);
            var html = await File.ReadAllTextAsync(paths.HtmlPath);
            await Assert.That(txt).Contains("2026-01-01");
            await Assert.That(txt).Contains("1.");
            await Assert.That(txt).Contains("|");
            await Assert.That(html).Contains("chapter-metadata");
            await Assert.That(File.Exists(paths.PdfPath)).IsTrue();
            await Assert.That(new FileInfo(paths.PdfPath).Length).IsGreaterThan(100);

            var catalog = new ManuscriptCatalog();
            var book = catalog.FindBook(root, "demo", "book-one")!;
            var paths2 = BookPrintExporter.ExportBook(book, Path.Combine(outDir, "via-book"), new BookPrintOptions
            {
                ShowAllMetadataTags = false,
            });
            var readerTxt = await File.ReadAllTextAsync(paths2.TextPath);
            await Assert.That(readerTxt).Contains("2026-01-01");
            await Assert.That(readerTxt).DoesNotContain("POV  ");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            if (Directory.Exists(outDir))
                Directory.Delete(outDir, recursive: true);
        }
    }

    [Test]
    public async Task BookPrintExporter_missing_directory_throws()
    {
        await Assert.That(() => BookPrintExporter.ExportBookFolder(
                Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}"),
                Path.GetTempPath(),
                "s",
                "b"))
            .ThrowsExactly<DirectoryNotFoundException>();
    }

    [Test]
    public async Task ReferenceManualExporter_nested_folders_and_export_set()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-ref-{Guid.NewGuid():N}");
        var refs = Path.Combine(root, "references");
        var outDir = Path.Combine(root, "out");
        try
        {
            Directory.CreateDirectory(Path.Combine(refs, "ships"));
            Directory.CreateDirectory(Path.Combine(refs, "history", "deep"));
            File.WriteAllText(Path.Combine(refs, "ships", "calypso.md"), "# Calypso\n\nShip notes.\n");
            File.WriteAllText(Path.Combine(refs, "history", "deep", "timeline.md"), "# Timeline\n\n- event\n");
            File.WriteAllText(Path.Combine(refs, "root-note.md"), "# Root\n\nBody.\n");

            var paths = ReferenceManualExporter.Export(
                refs,
                outDir,
                "demo-series",
                "Reference Manual",
                coverSubtitle: "Demo Cycle");
            var md = await File.ReadAllTextAsync(paths.MarkdownPath);
            await Assert.That(md).Contains("Ships");
            await Assert.That(md).Contains("History");
            await Assert.That(md).Contains("Calypso");
            await Assert.That(File.Exists(paths.PdfPath)).IsTrue();
            await Assert.That(new FileInfo(paths.PdfPath).Length).IsGreaterThan(100);

            var set = new ReferenceSetInfo(
                "ships",
                "Ships",
                Path.Combine(refs, "ships"),
                [new ReferenceFileInfo("calypso", "Calypso", Path.Combine(refs, "ships", "calypso.md"))]);
            var setPaths = ReferenceManualExporter.ExportSet(set, Path.Combine(outDir, "set"), seriesId: "demo");
            await Assert.That(File.Exists(setPaths.PdfPath)).IsTrue();

            ManuscriptBookPdfExporter.ExportReferenceSet(set, Path.Combine(outDir, "set-only.pdf"));
            await Assert.That(File.Exists(Path.Combine(outDir, "set-only.pdf"))).IsTrue();

            await Assert.That(() => ReferenceManualExporter.Export(
                    Path.Combine(root, "empty-refs"),
                    outDir,
                    "s",
                    "t"))
                .ThrowsExactly<DirectoryNotFoundException>();
            Directory.CreateDirectory(Path.Combine(root, "empty-refs"));
            await Assert.That(() => ReferenceManualExporter.Export(
                    Path.Combine(root, "empty-refs"),
                    outDir,
                    "s",
                    "t"))
                .ThrowsExactly<InvalidOperationException>();
            await Assert.That(() => ReferenceManualExporter.ExportSet(
                    new ReferenceSetInfo("empty", "Empty", refs, []),
                    outDir))
                .ThrowsExactly<InvalidOperationException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task ManuscriptPrintSettings_defaults_and_load_missing()
    {
        var defaults = ManuscriptPrintSettings.Load(Path.Combine(Path.GetTempPath(), $"no-print-{Guid.NewGuid():N}.json"));
        await Assert.That(defaults.BodyFontSize).IsGreaterThan(0);
        await Assert.That(defaults.IncludeCover).IsTrue();
    }

    static string CreateBookWorkspace(string chapterMarkdown, bool includeRights)
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-pdf-{Guid.NewGuid():N}");
        var series = Path.Combine(root, "content", "series", "demo");
        var book = Path.Combine(series, "books", "book-one");
        var chapters = Path.Combine(book, "chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(series, "series.yaml"), "id: demo\nname: Demo Series\n");
        File.WriteAllText(Path.Combine(book, "book.yaml"), includeRights
            ? "title: Book One\nsubtitle: Sub\nauthor: Auth\nrights: Copyright\n"
            : "title: Book One\n");
        File.WriteAllText(Path.Combine(chapters, "001-opening.md"), chapterMarkdown);
        File.WriteAllText(Path.Combine(chapters, "a-appendix.md"), "# Appendix A - Extra\n\nAppendix body.\n");
        return root;
    }
}
