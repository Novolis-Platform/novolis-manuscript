using Novolis.Manuscript;
using Novolis.Manuscript.Metrics;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptMetricsCoverageTests
{
    [Test]
    public async Task GetWordCount_strips_markdown_noise()
    {
        var md = """
            # Title
            See [link](https://x.test) and ![img](a.png).

            ```
            code block words ignored
            ```

            Real *words* here TODO FIXME TK.
            """;
        await Assert.That(ManuscriptMetrics.GetWordCount(md)).IsGreaterThan(3);
        await Assert.That(ManuscriptMetrics.GetWordCount("   ")).IsEqualTo(0);
    }

    [Test]
    public async Task Compute_and_write_reports_for_workspace()
    {
        var root = CreateWorkspace();
        try
        {
            var all = ManuscriptMetrics.ComputeAll(root);
            await Assert.That(all.Count).IsGreaterThanOrEqualTo(2);
            await Assert.That(all.Any(b => b.Series == "demo")).IsTrue();
            await Assert.That(all.Any(b => b.Series == "books")).IsTrue();

            var one = ManuscriptMetrics.ComputeOne(root, "demo", "book-one");
            await Assert.That(one.TotalWords).IsGreaterThan(0);
            await Assert.That(one.TargetWords).IsEqualTo(50000);
            await Assert.That(one.TotalTodos).IsGreaterThan(0);
            await Assert.That(one.Chapters.Count).IsGreaterThan(0);

            var md = ManuscriptMetrics.FormatMarkdown(one);
            await Assert.That(md).Contains("Target words");
            await Assert.That(md).Contains("| Chapter |");

            var runOne = ManuscriptMetrics.RunOne(root, "demo", "book-one");
            await Assert.That(runOne.Book).IsEqualTo("book-one");
            var metricsJson = Path.Combine(root, "out", "demo", "book-one", "metrics", "book-one.metrics.json");
            await Assert.That(File.Exists(metricsJson)).IsTrue();

            var runAll = ManuscriptMetrics.RunAll(root);
            await Assert.That(runAll.Count).IsEqualTo(all.Count);
            await Assert.That(File.Exists(Path.Combine(root, "out", "metrics", "overview.metrics.md"))).IsTrue();
            await Assert.That(File.Exists(Path.Combine(root, "out", "lone", "metrics", "lone.metrics.json"))).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task ComputeAll_rejects_non_workspace()
    {
        var temp = Path.Combine(Path.GetTempPath(), $"ms-metrics-bad-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        try
        {
            await Assert.That(() => ManuscriptMetrics.ComputeAll(temp))
                .ThrowsExactly<InvalidOperationException>();
            await Assert.That(() => ManuscriptMetrics.ComputeOne(temp, "x", "y"))
                .ThrowsExactly<InvalidOperationException>();
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Test]
    public async Task CharacterSlices_workspace_json_and_missing_metadata()
    {
        var root = CreateWorkspace();
        try
        {
            var chapters = Path.Combine(root, "content", "series", "demo", "books", "book-one", "chapters");
            File.WriteAllText(Path.Combine(chapters, "003-gap.md"), "# Chapter 3 - Gap\n\nNo metadata here.\n");
            File.WriteAllText(Path.Combine(chapters, "skip-me.md"), "plain note without heading number\n");

            var report = ManuscriptCharacterSlices.BuildFromWorkspace(root, "demo", "book-one");
            await Assert.That(report.MissingPov.Count).IsGreaterThan(0);
            await Assert.That(report.MissingCharacters.Count).IsGreaterThan(0);

            var json = report.ToJson();
            await Assert.That(json).Contains("\"label\"");
            var filtered = report.ToJson("Ryn");
            await Assert.That(filtered).Contains("Ryn");
            await Assert.That(() => report.ToJson("Nobody"))
                .ThrowsExactly<InvalidOperationException>();
            await Assert.That(() => report.ToMarkdown("Nobody"))
                .ThrowsExactly<InvalidOperationException>();

            var book = new ManuscriptCatalog().FindBook(root, "demo", "book-one")!;
            var viaBook = ManuscriptCharacterSlices.Build(book);
            await Assert.That(viaBook.Chapters.Count).IsEqualTo(report.Chapters.Count);

            var md = report.ToMarkdown();
            await Assert.That(md).Contains("### Missing POV");
            await Assert.That(md).Contains("### Missing Characters");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string CreateWorkspace()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-metrics-{Guid.NewGuid():N}");
        var series = Path.Combine(root, "content", "series", "demo");
        var book = Path.Combine(series, "books", "book-one");
        var chapters = Path.Combine(book, "chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(series, "series.yaml"), "id: demo\nname: Demo\n");
        File.WriteAllText(Path.Combine(book, "book.yaml"), """
            title: Book One
            author: Test
            targets:
              words: 50000
            """);
        File.WriteAllText(Path.Combine(chapters, "001-alpha.md"), """
            # Chapter 1 - Alpha

            > [!pov] Ryn
            > [!characters] Ryn, Tess

            Hello alpha with enough words for counting TODO.
            """);
        File.WriteAllText(Path.Combine(chapters, "002-beta.md"), """
            # Chapter 2 - Beta

            > [!pov] Tess
            > [!characters] Tess; Ryn

            Hello beta FIXME.
            """);

        var lone = Path.Combine(root, "content", "books", "lone");
        Directory.CreateDirectory(Path.Combine(lone, "chapters"));
        File.WriteAllText(Path.Combine(lone, "book.yaml"), "title: Lone\ntarget_words: 1000\n");
        File.WriteAllText(Path.Combine(lone, "chapters", "001.md"), "# Chapter 1 - Only\n\nBody words here.\n");
        return root;
    }
}
