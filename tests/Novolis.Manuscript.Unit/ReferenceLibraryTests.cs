using Novolis.IO.Indexing;
using Novolis.Manuscript;
using Novolis.Manuscript.References;

namespace Novolis.Manuscript.Unit;

public sealed class ReferenceLibraryTests
{
    [Test]
    public async Task Catalog_set_registers_cards_without_reading_files()
    {
        var set = new ReferenceSetInfo(
            "lore",
            "Lore",
            @"D:\refs\lore",
            [
                new ReferenceFileInfo("calypso", "Calypso", @"D:\refs\lore\calypso.md"),
                new ReferenceFileInfo("mira", "Mira", @"D:\refs\lore\mira.md"),
            ]);

        var library = new ReferenceLibraryBuilder()
            .AddReferenceSet(set, seriesId: "calypso")
            .Build();

        await Assert.That(library.TryResolve("calypso", out var card)).IsTrue();
        await Assert.That(card.Title).IsEqualTo("Calypso");
        await Assert.That(library.CardsInSet("lore").Count()).IsGreaterThanOrEqualTo(2);
        await Assert.That(library.Index.TryGetDocument("calypso", out var doc)).IsTrue();
        await Assert.That(doc.Location).IsEqualTo(@"D:\refs\lore\calypso.md");
        await Assert.That(library.CardsByFacet(ReferenceFacets.Series, "calypso").Any()).IsTrue();
    }

    [Test]
    public async Task Mentions_support_aliases_and_unresolved_targets()
    {
        var library = new ReferenceLibraryBuilder()
            .AddCard("calypso", aliases: ["the tramp"])
            .Mention("ch1", "the tramp", span: new IndexSpan(0, 9))
            .Mention("ch1", "missing-card")
            .SeeAlso("calypso", "mira")
            .Build();

        var mentions = library.MentionsFrom("ch1").ToArray();
        await Assert.That(mentions.Length).IsEqualTo(2);
        await Assert.That(library.UnresolvedMentionTargets().ToArray()).IsEquivalentTo(["missing-card"]);
        await Assert.That(library.TryResolve("the tramp", out _)).IsTrue();
    }

    [Test]
    public async Task AddSeries_and_AddBook_register_sets()
    {
        var set = new ReferenceSetInfo(
            "ships",
            "Ships",
            @"D:\refs\ships",
            [new ReferenceFileInfo("calypso", "Calypso", @"D:\refs\ships\calypso.md")]);
        var series = new SeriesInfo("demo", "Demo", @"D:\series", [], [set]);
        var book = new BookInfo(
            "b1",
            "Book",
            null,
            null,
            @"D:\book",
            "demo",
            [],
            false,
            false,
            [set]);

        var fromSeries = new ReferenceLibraryBuilder().AddSeries(series).Build();
        await Assert.That(fromSeries.TryResolve("calypso", out _)).IsTrue();

        var fromBook = new ReferenceLibraryBuilder().AddBook(book).Build();
        await Assert.That(fromBook.CardsInSet("ships").Any()).IsTrue();

        await Assert.That(() => new ReferenceLibrary(null!)).ThrowsExactly<ArgumentNullException>();
    }
}
