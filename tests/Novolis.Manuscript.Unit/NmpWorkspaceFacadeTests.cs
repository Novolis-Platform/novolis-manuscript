using Novolis.Manuscript;

namespace Novolis.Manuscript.Unit;

public sealed class NmpWorkspaceFacadeTests
{
    [Test]
    public async Task TryOpen_And_Load_Nmp_Fixture()
    {
        var root = Directory.CreateTempSubdirectory("nmp-facade-").FullName;
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
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Book One\norder: 1\n");
            File.WriteAllText(Path.Combine(chapters, "10-alpha.md"), "# Alpha\n\nBody.\n");
            File.WriteAllText(Path.Combine(chapters, "20-beta.md"), "# Beta\n\nBody.\n");

            await Assert.That(ManuscriptWorkspace.TryOpen(book, out var ws)).IsTrue();
            await Assert.That(ws).IsNotNull();
            await Assert.That(ws!.IsProtocolLayout).IsTrue();
            await Assert.That(ws.ContentRoot).IsEqualTo(Path.GetFullPath(root));

            var seriesList = ws.Catalog.Load(ws.ContentRoot);
            await Assert.That(seriesList.Count).IsEqualTo(1);
            await Assert.That(seriesList[0].Id).IsEqualTo("s1");
            await Assert.That(seriesList[0].Books.Count).IsEqualTo(1);
            await Assert.That(seriesList[0].Books[0].Chapters.Count).IsEqualTo(2);
            await Assert.That(seriesList[0].Books[0].Chapters[0].Id).IsEqualTo("10-alpha");
            await Assert.That(seriesList[0].Books[0].ChapterOrderFromHeading).IsFalse();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
