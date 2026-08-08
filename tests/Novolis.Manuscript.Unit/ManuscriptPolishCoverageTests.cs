using Novolis.Manuscript.Editorial;
using Novolis.Manuscript.Export.Pdf;
using Novolis.Manuscript.LegacyBooks;
using Novolis.Manuscript.Protocol;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptPolishCoverageTests
{
    [Test]
    public async Task Stylesheet_and_blank_metadata_export()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-style-{Guid.NewGuid():N}");
        var outDir = Path.Combine(root, "out");
        try
        {
            var book = Path.Combine(root, "content", "books", "demo");
            Directory.CreateDirectory(Path.Combine(book, "chapters"));
            Directory.CreateDirectory(Path.Combine(root, "style"));
            File.WriteAllText(Path.Combine(root, "style.css"), "body{font-family:serif}");
            File.WriteAllText(Path.Combine(root, "style", "style.css"), "body{color:#111}");
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Demo\n");
            File.WriteAllText(Path.Combine(book, "chapters", "01.md"), """
                # Chapter 1 - Meta

                > [!date]   
                > [!time] 10:00
                > [!location] Dock

                Paragraph with nested:
                - item
                  - nested

                1. one
                   1. nested ordered

                Body.
                """);
            var paths = BookPrintExporter.ExportBookFolder(book, outDir, "", "demo");
            await Assert.That(File.Exists(paths.HtmlPath)).IsTrue();
            var html = await File.ReadAllTextAsync(paths.HtmlPath);
            await Assert.That(html.Length).IsGreaterThan(50);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Editorial_slop_addsimple_patterns()
    {
        var text = """
            Such was the nature of power in the room.
            She felt a sense of dread settle in.
            He explained how systems work in practice.
            The result? Absolute chaos everywhere.
            Something shifted.
            Not this. Not that. Just clarity.
            """;
        var findings = EditorialAnalyzer.AnalyzeText(text);
        await Assert.That(findings.Count).IsGreaterThan(0);
        var longNegation = "Not " + new string('x', 70) + ". Just Something else starts here.\n";
        var longFindings = SlopPatternRules.Scan(longNegation, "long.md");
        await Assert.That(longFindings.Count).IsGreaterThan(0);
        var slop = SlopPatternRules.Scan(text, "x.md");
        await Assert.That(slop.Count).IsGreaterThan(3);
    }

    [Test]
    public async Task Legacy_empty_refs_bom_and_time_callout()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-leg3-{Guid.NewGuid():N}");
        try
        {
            var series = Path.Combine(root, "content", "series", "demo");
            var book = Path.Combine(series, "books", "one");
            Directory.CreateDirectory(Path.Combine(book, "chapters"));
            Directory.CreateDirectory(Path.Combine(series, "references"));
            File.WriteAllText(Path.Combine(series, "series.yaml"), "id: demo\nname: Demo\n");
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: One\n");
            File.WriteAllText(Path.Combine(book, "chapters", "01-alpha.md"),
                "\uFEFF<!-- booktools-chapter: 1 -->\n\n# Chapter 1 - Alpha\n\n> [!time] 12:00\n\nBody.\n");
            File.WriteAllText(Path.Combine(book, "chapters", "no-key.md"), "\uFEFF# Untitled note\n\n");

            var snapshot = new LegacyBooksCatalogReader().Read(root);
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.EmptyReferenceFolder)).IsTrue();
            await Assert.That(snapshot.Catalog.Fiction[0].Series[0].Books[0].Chapters.Count).IsGreaterThan(0);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
