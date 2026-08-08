using Novolis.Manuscript;
using Novolis.Manuscript.Export.Pdf;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptCatalogTests
{
    [Test]
    public async Task Load_SeriesAndStandalone()
    {
        var root = CreateFixture();
        try
        {
            var catalog = new ManuscriptCatalog();
            var series = catalog.Load(root);
            await Assert.That(series.Count).IsEqualTo(1);
            await Assert.That(series[0].Id).IsEqualTo("demo");
            await Assert.That(series[0].Books.Count).IsEqualTo(1);
            await Assert.That(series[0].Books[0].Chapters.Count).IsEqualTo(2);
            await Assert.That(series[0].Books[0].Chapters[0].Title).IsEqualTo("Alpha");

            var standalone = catalog.LoadStandaloneBooks(root);
            await Assert.That(standalone.Count).IsEqualTo(1);
            await Assert.That(standalone[0].Id).IsEqualTo("lone");

            var found = catalog.FindBook(root, "demo", "book-one");
            await Assert.That(found).IsNotNull();
            await Assert.That(found!.Title).IsEqualTo("Book One");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Workspace_TryOpen()
    {
        var root = CreateFixture();
        try
        {
            var nested = Path.Combine(root, "content", "series", "demo");
            await Assert.That(ManuscriptWorkspace.TryOpen(nested, out var ws)).IsTrue();
            await Assert.That(ws!.ContentRoot).IsEqualTo(Path.GetFullPath(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Doctor_ReportsMissingYamlAndEmptyChapter()
    {
        var root = CreateFixture();
        try
        {
            var bookDir = Path.Combine(root, "content", "series", "demo", "books", "book-one");
            File.Delete(Path.Combine(bookDir, "book.yaml"));
            File.WriteAllText(Path.Combine(bookDir, "chapters", "003-empty.md"), "   ");

            var findings = ManuscriptDoctor.Diagnose(root);
            await Assert.That(findings.Any(f => f.Code == "missing-book-yaml")).IsTrue();
            await Assert.That(findings.Any(f => f.Code == "empty-chapter")).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task ExportBook_WritesPdf()
    {
        var root = CreateFixture();
        var pdf = Path.Combine(Path.GetTempPath(), $"ms-book-{Guid.NewGuid():N}.pdf");
        try
        {
            var book = new ManuscriptCatalog().FindBook(root, "demo", "book-one")!;
            ManuscriptBookPdfExporter.ExportBook(book, pdf, new ManuscriptPrintSettings { IncludeCover = true });
            await Assert.That(File.Exists(pdf)).IsTrue();
            var bytes = File.ReadAllBytes(pdf);
            await Assert.That(bytes.Length).IsGreaterThan(100);
            var header = bytes.Take(4).ToArray();
            await Assert.That(System.Text.Encoding.ASCII.GetString(header)).IsEqualTo("%PDF");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            if (File.Exists(pdf))
                File.Delete(pdf);
        }
    }

    [Test]
    public async Task ExportBookFolder_WritesCompanionsAndPdf()
    {
        var root = CreateFixture();
        var outDir = Path.Combine(Path.GetTempPath(), $"ms-print-{Guid.NewGuid():N}");
        try
        {
            var bookDir = Path.Combine(root, "content", "series", "demo", "books", "book-one");
            var paths = BookPrintExporter.ExportBookFolder(bookDir, outDir, "demo", "book-one");
            await Assert.That(File.Exists(paths.MarkdownPath)).IsTrue();
            await Assert.That(File.Exists(paths.HtmlPath)).IsTrue();
            await Assert.That(File.Exists(paths.TextPath)).IsTrue();
            await Assert.That(File.Exists(paths.PdfPath)).IsTrue();
            await Assert.That(new FileInfo(paths.PdfPath).Length).IsGreaterThan(100);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            if (Directory.Exists(outDir))
                Directory.Delete(outDir, recursive: true);
        }
    }

    static string CreateFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-catalog-{Guid.NewGuid():N}");
        var seriesDir = Path.Combine(root, "content", "series", "demo");
        var bookDir = Path.Combine(seriesDir, "books", "book-one");
        var chapters = Path.Combine(bookDir, "chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(seriesDir, "series.yaml"), "id: demo\nname: Demo Series\n");
        File.WriteAllText(Path.Combine(bookDir, "book.yaml"), "title: Book One\nauthor: Test\n");
        File.WriteAllText(Path.Combine(chapters, "001-alpha.md"), "# Chapter 1 - Alpha\n\n> [!pov] Ryn\n\nHello alpha.\n");
        File.WriteAllText(Path.Combine(chapters, "002-beta.md"), "# Chapter 2 - Beta\n\nHello beta.\n");

        var lone = Path.Combine(root, "content", "books", "lone");
        Directory.CreateDirectory(Path.Combine(lone, "chapters"));
        File.WriteAllText(Path.Combine(lone, "book.yaml"), "title: Lone Book\n");
        File.WriteAllText(Path.Combine(lone, "chapters", "001.md"), "# Chapter 1 - Only\n\nBody.\n");
        return root;
    }
}
