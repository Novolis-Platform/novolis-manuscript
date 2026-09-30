using Novolis.Markup.Markdown;

namespace Novolis.Manuscript.Unit;

public sealed class BookDocumentTests
{
    [Test]
    public async Task Open_protocol_parses_each_chapter_as_markdown()
    {
        var root = Directory.CreateTempSubdirectory("book-doc-nmp-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(root, "manuscript.yaml"), """
                protocol: novolis.manuscript
                version: 1
                """);
            var series = Path.Combine(root, "src", "Fiction", "u1", "s1");
            var book = Path.Combine(series, "b1");
            var chapters = Path.Combine(book, "Chapters");
            Directory.CreateDirectory(chapters);
            File.WriteAllText(Path.Combine(root, "src", "Fiction", "u1", "universe.yaml"), "title: U\n");
            File.WriteAllText(Path.Combine(series, "series.yaml"), "title: Series One\n");
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Book One\nsubtitle: Sub\nauthors: [Auth]\nrights: Mine\norder: 1\n");
            File.WriteAllText(Path.Combine(chapters, "10-alpha.md"), """
                ---
                status: draft
                ---
                # Alpha

                > 2026-01-01 08:00

                Alpha body.
                """);
            File.WriteAllText(Path.Combine(chapters, "20-beta.md"), "# Beta\n\nBeta body.\n");

            var opened = BookDocument.Open(book);
            await Assert.That(opened.Id).IsEqualTo("b1");
            await Assert.That(opened.Title).IsEqualTo("Book One");
            await Assert.That(opened.Subtitle).IsEqualTo("Sub");
            await Assert.That(opened.Author).IsEqualTo("Auth");
            await Assert.That(opened.SeriesId).IsEqualTo("s1");
            await Assert.That(opened.SeriesTitle).IsEqualTo("Series One");
            await Assert.That(opened.Rights).IsEqualTo("Mine");
            await Assert.That(opened.Chapters.Count).IsEqualTo(2);
            await Assert.That(opened.Chapters[0].Title).IsEqualTo("Alpha");
            await Assert.That(opened.Chapters[0].Slug).IsEqualTo("alpha");
            await Assert.That(opened.Chapters[1].Title).IsEqualTo("Beta");
            await Assert.That(opened.Chapters[0].Body.Any(s => s is IMarkdownHeader { Text: "Alpha" })).IsTrue();
            await Assert.That(opened.Chapters[0].Body.Any(s => s is IMarkdownQuote)).IsTrue();
            await Assert.That(opened.Chapters[0].Body.ToString()).DoesNotContain("status:");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public async Task Open_legacy_scans_lowercase_chapters()
    {
        var root = Directory.CreateTempSubdirectory("book-doc-legacy-").FullName;
        try
        {
            var book = Path.Combine(root, "content", "series", "demo", "books", "book-one");
            var chapters = Path.Combine(book, "chapters");
            Directory.CreateDirectory(chapters);
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Book One\nauthor: Auth\n");
            File.WriteAllText(Path.Combine(chapters, "001-opening.md"), "# Opening\n\nFirst.\n");
            File.WriteAllText(Path.Combine(chapters, "002-watch.md"), "# The Watch\n\nSecond.\n");

            var opened = BookDocument.Open(book);
            await Assert.That(opened.Chapters.Count).IsEqualTo(2);
            await Assert.That(opened.Chapters[0].Title).IsEqualTo("Opening");
            await Assert.That(opened.Chapters[1].Title).IsEqualTo("The Watch");
            await Assert.That(opened.Chapters[0].Order).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
