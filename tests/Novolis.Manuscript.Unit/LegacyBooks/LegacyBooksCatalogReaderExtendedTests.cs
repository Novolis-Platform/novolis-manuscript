using Novolis.Manuscript.LegacyBooks;
using Novolis.Manuscript.Protocol;

namespace Novolis.Manuscript.Unit.LegacyBooks;

public sealed class LegacyBooksCatalogReaderExtendedTests
{
    [Test]
    public async Task Read_CalloutMetadata_MapsToChapterFields()
    {
        var root = CreateRoot();
        try
        {
            var book = Path.Combine(root, "content", "series", "demo", "books", "one");
            Directory.CreateDirectory(Path.Combine(book, "chapters"));
            File.WriteAllText(Path.Combine(root, "content", "series", "demo", "series.yaml"), """
                id: demo
                name: Demo
                """);
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: One\n");
            File.WriteAllText(Path.Combine(book, "chapters", "001.md"), """
                # Chapter 1 - Alpha

                > [!date] 2496.001
                > [!system] Hub
                > [!location] Dock
                > [!pov] Marsh
                > [!characters] Marsh, Ryn
                > [!status] draft

                Body.
                """);

            var chapter = new LegacyBooksCatalogReader().Read(root)
                .Catalog.Fiction[0].Series[0].Books[0].Chapters[0];

            await Assert.That(chapter.Title).IsEqualTo("Alpha");
            await Assert.That(chapter.Metadata.Date).IsEqualTo("2496.001");
            await Assert.That(chapter.Metadata.System).IsEqualTo("Hub");
            await Assert.That(chapter.Metadata.Locations![0]).IsEqualTo("Dock");
            await Assert.That(chapter.Metadata.Pov).IsEqualTo("Marsh");
            await Assert.That(chapter.Metadata.Status).IsEqualTo("draft");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Read_YamlFrontMatter_Lists()
    {
        var root = CreateRoot();
        try
        {
            var book = Path.Combine(root, "content", "books", "nf");
            Directory.CreateDirectory(Path.Combine(book, "chapters"));
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: NF\n");
            File.WriteAllText(Path.Combine(book, "chapters", "01-intro.md"), """
                ---
                locations:
                  - A
                  - B
                characters:
                  - X
                status: draft
                ---

                # Intro

                Body.
                """);

            var chapter = new LegacyBooksCatalogReader().Read(root)
                .Catalog.NonFiction[0].Books[0].Chapters[0];

            await Assert.That(chapter.Metadata.Locations!.Count).IsEqualTo(2);
            await Assert.That(chapter.Metadata.Characters![0]).IsEqualTo("X");
            await Assert.That(chapter.Metadata.Status).IsEqualTo("draft");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Read_OrderFromHeading_SortsByChapterNumber()
    {
        var root = CreateRoot();
        try
        {
            var book = Path.Combine(root, "content", "series", "demo", "books", "one");
            Directory.CreateDirectory(Path.Combine(book, "chapters"));
            File.WriteAllText(Path.Combine(root, "content", "series", "demo", "series.yaml"), """
                id: demo
                name: Demo
                """);
            File.WriteAllText(Path.Combine(book, "book.yaml"), """
                title: One
                chapter_order_from_heading: true
                """);
            File.WriteAllText(Path.Combine(book, "chapters", "zzz.md"), "# Chapter 2 - Second\n\nB.\n");
            File.WriteAllText(Path.Combine(book, "chapters", "aaa.md"), "# Chapter 1 - First\n\nA.\n");

            var chapters = new LegacyBooksCatalogReader().Read(root)
                .Catalog.Fiction[0].Series[0].Books[0].Chapters;

            await Assert.That(chapters[0].Title).IsEqualTo("First");
            await Assert.That(chapters[1].Title).IsEqualTo("Second");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Read_Appendices_AndEmptyBookWarning()
    {
        var root = CreateRoot();
        try
        {
            var book = Path.Combine(root, "content", "books", "emptyish");
            Directory.CreateDirectory(Path.Combine(book, "chapters"));
            Directory.CreateDirectory(Path.Combine(book, "appendices"));
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Emptyish\n");
            File.WriteAllText(Path.Combine(book, "appendices", "a1.md"), "# App\n\nA.\n");

            var snapshot = new LegacyBooksCatalogReader().Read(root);
            var loaded = snapshot.Catalog.NonFiction[0].Books[0];
            await Assert.That(loaded.Appendices.Count).IsEqualTo(1);
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.EmptyBook)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Read_MissingRoot_Throws()
    {
        var missing = Path.Combine(Path.GetTempPath(), "missing-legacy-" + Guid.NewGuid().ToString("N"));
        var threw = false;
        try
        {
            new LegacyBooksCatalogReader().Read(missing);
        }
        catch (DirectoryNotFoundException)
        {
            threw = true;
        }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task Read_SkipsUnderscoreArchiveReferences()
    {
        var root = CreateRoot();
        try
        {
            var series = Path.Combine(root, "content", "series", "demo");
            Directory.CreateDirectory(Path.Combine(series, "books", "one", "chapters"));
            Directory.CreateDirectory(Path.Combine(series, "references", "_archive"));
            Directory.CreateDirectory(Path.Combine(series, "references", "ships"));
            File.WriteAllText(Path.Combine(series, "series.yaml"), "id: demo\nname: Demo\n");
            File.WriteAllText(Path.Combine(series, "books", "one", "book.yaml"), "title: One\n");
            File.WriteAllText(Path.Combine(series, "books", "one", "chapters", "1.md"), "# One\n\nB.\n");
            File.WriteAllText(Path.Combine(series, "references", "_archive", "old.md"), "# Old\n\nX.\n");
            File.WriteAllText(Path.Combine(series, "references", "ships", "calypso.md"), "# Calypso\n\nShip.\n");

            var refs = new LegacyBooksCatalogReader().Read(root)
                .Catalog.Fiction[0].Series[0].References;

            await Assert.That(refs.Count).IsEqualTo(1);
            await Assert.That(refs[0].Id).Contains("ships/calypso");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "legacy-ext-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
