using Novolis.Manuscript;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptCatalogExtendedTests
{
    [Test]
    public async Task Load_LoadsSeriesWithChaptersAndAppendices()
    {
        var root = CreateSeriesFixture();
        try
        {
            var catalog = new ManuscriptCatalog().Load(root);
            await Assert.That(catalog.Count).IsEqualTo(1);
            await Assert.That(catalog[0].Id).IsEqualTo("demo");
            await Assert.That(catalog[0].Books[0].Chapters.Count).IsEqualTo(3);
            await Assert.That(catalog[0].Books[0].Chapters.Any(c => c.Kind == ChapterKind.Appendix)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task FindBook_LocatesSeriesAndStandalone()
    {
        var root = CreateSeriesFixture();
        var standaloneDir = Path.Combine(root, "content", "books", "solo");
        Directory.CreateDirectory(Path.Combine(standaloneDir, "chapters"));
        File.WriteAllText(Path.Combine(standaloneDir, "book.yaml"), "title: Solo\nauthor: Me\n");
        File.WriteAllText(Path.Combine(standaloneDir, "chapters", "001.md"), "# Solo\n");
        try
        {
            var catalog = new ManuscriptCatalog();
            var seriesBook = catalog.FindBook(root, "demo", "book-one");
            await Assert.That(seriesBook).IsNotNull();
            await Assert.That(seriesBook!.Title).IsEqualTo("Book One");

            var standalone = catalog.FindBook(root, null, "solo");
            await Assert.That(standalone).IsNotNull();
            await Assert.That(standalone!.Author).IsEqualTo("Me");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task LoadBook_IncludesReferenceSets()
    {
        var root = CreateSeriesFixture(includeReferences: true);
        try
        {
            var book = new ManuscriptCatalog().FindBook(
                root, "demo", "book-one");
            await Assert.That(book).IsNotNull();
            await Assert.That(book!.References.Count).IsEqualTo(1);
            await Assert.That(book.References[0].Files.Count).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateSeriesFixture(bool includeReferences = false)
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-cat-{Guid.NewGuid():N}");
        var bookDir = Path.Combine(root, "content", "series", "demo", "books", "book-one");
        Directory.CreateDirectory(Path.Combine(bookDir, "chapters"));
        Directory.CreateDirectory(Path.Combine(bookDir, "appendices"));
        File.WriteAllText(Path.Combine(root, "content", "series", "demo", "series.yaml"), "id: demo\nname: Demo Series\n");
        File.WriteAllText(Path.Combine(bookDir, "book.yaml"), "title: Book One\nsubtitle: Sub\nchapter_order_from_heading: true\n");
        File.WriteAllText(Path.Combine(bookDir, "chapters", "001-alpha.md"), "# Alpha\n\nBody.");
        File.WriteAllText(Path.Combine(bookDir, "chapters", "002-beta.md"), "# Beta\n\nMore.");
        File.WriteAllText(Path.Combine(bookDir, "appendices", "a1-glossary.md"), "# Glossary\n\nTerms.");
        if (includeReferences)
        {
            var refDir = Path.Combine(bookDir, "references", "notes");
            Directory.CreateDirectory(refDir);
            File.WriteAllText(Path.Combine(refDir, "note.md"), "# Note\n");
        }

        return root;
    }
}
