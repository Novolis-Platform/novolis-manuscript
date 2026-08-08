using Novolis.Manuscript.LegacyBooks;
using Novolis.Manuscript.Protocol;

namespace Novolis.Manuscript.Unit.LegacyBooks;

public sealed class LegacyBooksCatalogReaderTests
{
    [Test]
    public async Task Read_MapsSeriesAndStandalone()
    {
        var root = CreateLegacyFixture();
        try
        {
            var snapshot = new LegacyBooksCatalogReader().Read(root);

            await Assert.That(snapshot.Catalog.Fiction.Count).IsEqualTo(1);
            var universe = snapshot.Catalog.Fiction[0];
            await Assert.That(universe.Id).IsEqualTo("legacy");
            await Assert.That(universe.Series.Count).IsEqualTo(1);
            await Assert.That(universe.Series[0].Id).IsEqualTo("the-calypso-cycle");
            await Assert.That(universe.Series[0].Metadata.Title).IsEqualTo("The Calypso Cycle");
            await Assert.That(universe.Series[0].Books.Count).IsEqualTo(1);

            var book = universe.Series[0].Books[0];
            await Assert.That(book.Address.BookId).IsEqualTo("calypso");
            await Assert.That(book.Chapters.Count).IsEqualTo(2);
            await Assert.That(book.Chapters[0].Title).IsEqualTo("Alpha");

            await Assert.That(snapshot.Catalog.NonFiction.Count).IsEqualTo(1);
            await Assert.That(snapshot.Catalog.NonFiction[0].Books.Count).IsEqualTo(1);
            await Assert.That(snapshot.Catalog.NonFiction[0].Books[0].Address.BookId)
                .IsEqualTo("intro-to-programming");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Read_AcceptsReferenceSingularAlias()
    {
        var root = CreateLegacyFixture();
        try
        {
            var refDir = Path.Combine(root, "content", "series", "the-calypso-cycle", "reference", "ships");
            Directory.CreateDirectory(refDir);
            File.WriteAllText(Path.Combine(refDir, "calypso.md"), "# Calypso\n\nShip.\n");

            // Remove plural so singular is used
            var plural = Path.Combine(root, "content", "series", "the-calypso-cycle", "references");
            if (Directory.Exists(plural))
                Directory.Delete(plural, recursive: true);

            var snapshot = new LegacyBooksCatalogReader().Read(root);
            await Assert.That(snapshot.Catalog.Fiction[0].Series[0].References.Count).IsEqualTo(1);
            await Assert.That(snapshot.Catalog.Fiction[0].Series[0].References[0].Id)
                .IsEqualTo("fiction/legacy/the-calypso-cycle/reference/ships/calypso");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string CreateLegacyFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "legacy-books-" + Guid.NewGuid().ToString("N"));
        var series = Path.Combine(root, "content", "series", "the-calypso-cycle");
        var book = Path.Combine(series, "books", "calypso");
        Directory.CreateDirectory(Path.Combine(book, "chapters"));
        Directory.CreateDirectory(Path.Combine(series, "references", "continuity"));

        File.WriteAllText(Path.Combine(series, "series.yaml"), """
            id: the-calypso-cycle
            name: The Calypso Cycle
            """);
        File.WriteAllText(Path.Combine(book, "book.yaml"), """
            title: Calypso
            series: The Calypso Cycle
            chapter_order_from_heading: true
            debug_mode: false
            """);
        File.WriteAllText(Path.Combine(book, "chapters", "001-alpha.md"), """
            # Chapter 1 - Alpha

            > [!pov] Marsh

            Body.
            """);
        File.WriteAllText(Path.Combine(book, "chapters", "002-beta.md"), """
            # Chapter 2 - Beta

            Body.
            """);
        File.WriteAllText(Path.Combine(series, "references", "continuity", "timeline.md"), """
            # Timeline

            Notes.
            """);

        var standalone = Path.Combine(root, "content", "books", "intro-to-programming");
        Directory.CreateDirectory(Path.Combine(standalone, "chapters"));
        File.WriteAllText(Path.Combine(standalone, "book.yaml"), """
            title: Intro to Programming
            language: C# 13
            """);
        File.WriteAllText(Path.Combine(standalone, "chapters", "01-hello.md"), """
            # Hello

            World.
            """);

        return root;
    }
}
