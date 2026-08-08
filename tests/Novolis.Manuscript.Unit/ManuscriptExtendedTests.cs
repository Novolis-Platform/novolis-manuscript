using Novolis.Manuscript;
using Novolis.Manuscript.Export.Pdf;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptExtendedTests
{
    [Test]
    public async Task Metadata_ParseYamlFrontMatter()
    {
        var text = """
            ---
            title: Alpha
            date: 2026-01-01
            ---
            # Chapter 1 - Opening

            Body text.
            """;
        var (meta, body, format) = ManuscriptMetadata.Parse(text);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Yaml);
        await Assert.That(meta.Title).IsEqualTo("Opening");
        await Assert.That(body).Contains("Body text.");
    }

    [Test]
    public async Task PrintSettings_LoadDefaultsWhenMissing()
    {
        var settings = ManuscriptPrintSettings.Load(null);
        await Assert.That(settings.IncludeCover).IsTrue();
        await Assert.That(settings.BodyFontSize).IsEqualTo(11f);

        var pdf = settings.ToPdfOptions("Title", "Sub", "Author");
        await Assert.That(pdf.Title).IsEqualTo("Title");
        await Assert.That(pdf.IncludeCoverPage).IsTrue();
    }

    [Test]
    public async Task BookYaml_LoadsTitleFromFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"book-{Guid.NewGuid():N}.yaml");
        try
        {
            File.WriteAllText(path, "title: Example\nsubtitle: Sub\n");
            var dict = BookYaml.LoadFile(path);
            await Assert.That(BookYaml.GetString(dict, "title")).IsEqualTo("Example");
            await Assert.That(BookYaml.GetString(dict, "subtitle")).IsEqualTo("Sub");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task Catalog_LoadStandaloneBook()
    {
        var root = CreateFixture();
        try
        {
            var standaloneDir = Path.Combine(root, "content", "books", "lone");
            Directory.CreateDirectory(Path.Combine(standaloneDir, "chapters"));
            File.WriteAllText(Path.Combine(standaloneDir, "book.yaml"), "title: Lone Book\n");
            File.WriteAllText(Path.Combine(standaloneDir, "chapters", "001-one.md"), "# One\n");

            var books = new ManuscriptCatalog().LoadStandaloneBooks(root);
            await Assert.That(books.Count).IsEqualTo(1);
            await Assert.That(books[0].Id).IsEqualTo("lone");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-ext-{Guid.NewGuid():N}");
        var bookDir = Path.Combine(root, "content", "series", "demo", "books", "book-one");
        Directory.CreateDirectory(Path.Combine(bookDir, "chapters"));
        File.WriteAllText(Path.Combine(root, "content", "series", "demo", "series.yaml"), "id: demo\ntitle: Demo\n");
        File.WriteAllText(Path.Combine(bookDir, "book.yaml"), "title: Book One\n");
        File.WriteAllText(Path.Combine(bookDir, "chapters", "001-alpha.md"), "# Alpha\n\nBody.");
        File.WriteAllText(Path.Combine(bookDir, "chapters", "002-beta.md"), "# Beta\n\nMore.");
        return root;
    }
}
