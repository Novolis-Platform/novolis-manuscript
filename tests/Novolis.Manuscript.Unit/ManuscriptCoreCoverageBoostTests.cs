using Novolis.Manuscript;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptCoreCoverageBoostTests
{
    [Test]
    public async Task Ascii_normalize_and_scan_book_apis()
    {
        var root = CreateWorkspace();
        try
        {
            var chapters = Path.Combine(root, "content", "series", "demo", "books", "book-one", "chapters");
            var dirty = Path.Combine(chapters, "001-alpha.md");
            File.WriteAllText(dirty, "\uFEFFHe said\u2014\u201Cwait\u201D\u2026\n");

            var dry = ManuscriptAscii.NormalizeFile(dirty, dryRun: true, relax: true);
            await Assert.That(dry.Replacements).IsGreaterThan(0);
            await Assert.That(File.ReadAllText(dirty)).Contains('\u2014');

            var written = ManuscriptAscii.NormalizeFile(dirty, dryRun: false, relax: true);
            await Assert.That(written.Text).IsEqualTo("He said-\"wait\"...\n");
            await Assert.That(File.ReadAllText(dirty)).IsEqualTo(written.Text);

            File.WriteAllText(Path.Combine(chapters, "002-beta.md"), "emoji \u2603 left\n");
            var blocked = ManuscriptAscii.NormalizeFile(
                Path.Combine(chapters, "002-beta.md"), dryRun: false, relax: false);
            await Assert.That(blocked.HasRemainingNonAscii).IsTrue();

            var dirScan = ManuscriptAscii.ScanChaptersDirectory(chapters, limit: 10);
            await Assert.That(dirScan.Count).IsGreaterThan(0);

            var bookIssues = ManuscriptAscii.ScanBook(root, "demo", "book-one");
            await Assert.That(bookIssues.Count).IsGreaterThan(0);

            var book = new ManuscriptCatalog().FindBook(root, "demo", "book-one")!;
            var viaBook = ManuscriptAscii.ScanBook(book);
            await Assert.That(viaBook.Count).IsGreaterThan(0);

            var normalized = ManuscriptAscii.NormalizeBook(root, "demo", "book-one", dryRun: true, relax: true);
            await Assert.That(normalized.Count).IsGreaterThan(0);
            var normalizedBook = ManuscriptAscii.NormalizeBook(book, dryRun: true, relax: true);
            await Assert.That(normalizedBook.Count).IsEqualTo(normalized.Count);

            var dirNorm = ManuscriptAscii.NormalizeChaptersDirectory(chapters, dryRun: true, relax: true);
            await Assert.That(dirNorm.Count).IsEqualTo(normalized.Count);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Paths_resolve_from_book_and_yaml()
    {
        var root = CreateWorkspace();
        try
        {
            var book = new ManuscriptCatalog().FindBook(root, "demo", "book-one")!;
            var fromBook = ManuscriptPaths.ResolveChaptersDirectory(book);
            await Assert.That(Directory.Exists(fromBook)).IsTrue();

            var (resolvedBook, chapters) = ManuscriptPaths.ResolveBookChapters(root, "demo", "book-one");
            await Assert.That(resolvedBook.Id).IsEqualTo("book-one");
            await Assert.That(Directory.Exists(chapters)).IsTrue();

            var yaml = Path.Combine(book.DirectoryPath, "book.yaml");
            var fromYaml = ManuscriptPaths.ResolveChaptersDirectoryFromBookYaml(yaml);
            await Assert.That(fromYaml).IsEqualTo(Path.GetFullPath(fromBook));

            var lone = ManuscriptPaths.ResolveBookChapters(root, null, "lone");
            await Assert.That(lone.Book.Id).IsEqualTo("lone");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Doctor_duplicate_empty_ref_and_missing_order()
    {
        var root = CreateWorkspace();
        try
        {
            var seriesDir = Path.Combine(root, "content", "series", "demo");
            var bookDir = Path.Combine(seriesDir, "books", "book-one");
            File.WriteAllText(Path.Combine(bookDir, "book.yaml"), """
                title: Book One
                chapter_order_from_heading: true
                """);
            File.WriteAllText(Path.Combine(bookDir, "chapters", "dup-a.md"), "# Untitled\n\nBody.\n");
            File.WriteAllText(Path.Combine(bookDir, "chapters", "dup-b.md"), "# Also\n\nBody.\n");
            // Force duplicate stem by writing two files that catalog maps to same id pattern if possible —
            // catalog uses filename stem; create identical stems via nested rename is hard, so diagnose BookInfo directly.
            File.WriteAllText(Path.Combine(bookDir, "chapters", "empty.md"), "   \n");
            var chapters = new List<ChapterInfo>
            {
                new("same", "A", ChapterKind.Chapter, 1, Path.Combine(bookDir, "chapters", "001-alpha.md")),
                new("same", "B", ChapterKind.Chapter, 2, Path.Combine(bookDir, "chapters", "002-beta.md")),
                new("no-num", "C", ChapterKind.Chapter, double.PositiveInfinity, Path.Combine(bookDir, "chapters", "dup-a.md")),
                new("empty", "D", ChapterKind.Chapter, 3, Path.Combine(bookDir, "chapters", "empty.md")),
            };
            var book = new BookInfo(
                "book-one",
                "Book One",
                null,
                null,
                bookDir,
                "demo",
                chapters,
                ChapterOrderFromHeading: true,
                DebugMode: false,
                References: []);

            var findings = ManuscriptDoctor.Diagnose(book);
            await Assert.That(findings.Any(f => f.Code == "duplicate-chapter-stem")).IsTrue();
            await Assert.That(findings.Any(f => f.Code == "missing-chapter-order")).IsTrue();
            await Assert.That(findings.Any(f => f.Code == "empty-chapter")).IsTrue();

            Directory.CreateDirectory(Path.Combine(seriesDir, "references", "empty-set"));
            File.Delete(Path.Combine(seriesDir, "series.yaml"));
            var loaded = new ManuscriptCatalog().Load(root).Single();
            var seriesFindings = ManuscriptDoctor.Diagnose(new SeriesInfo(
                loaded.Id,
                loaded.Title,
                loaded.DirectoryPath,
                loaded.Books,
                [
                    new ReferenceSetInfo("empty-set", "Empty Set", Path.Combine(seriesDir, "references", "empty-set"), []),
                ]));
            await Assert.That(seriesFindings.Any(f => f.Code == "missing-series-yaml")).IsTrue();
            await Assert.That(seriesFindings.Any(f => f.Code == "empty-reference-set")).IsTrue();

            var nmpRoot = Path.Combine(Path.GetTempPath(), $"ms-nmp-bad-{Guid.NewGuid():N}");
            Directory.CreateDirectory(nmpRoot);
            File.WriteAllText(Path.Combine(nmpRoot, "manuscript.yaml"), "not: valid: {{{");
            Directory.CreateDirectory(Path.Combine(nmpRoot, "content", "books", "x", "chapters"));
            File.WriteAllText(Path.Combine(nmpRoot, "content", "books", "x", "book.yaml"), "title: X\n");
            File.WriteAllText(Path.Combine(nmpRoot, "content", "books", "x", "chapters", "01.md"), "# Chapter 1 - X\n\nHi.\n");
            var mixed = ManuscriptDoctor.Diagnose(nmpRoot);
            await Assert.That(mixed.Any(f => f.Code == "nmp-open-failed")).IsTrue();
            Directory.Delete(nmpRoot, recursive: true);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Workspace_try_open_from_content_root_and_book()
    {
        var root = CreateWorkspace();
        try
        {
            await Assert.That(ManuscriptWorkspace.TryOpen(root, out var ws)).IsTrue();
            await Assert.That(ws!.ContentRoot).IsEqualTo(Path.GetFullPath(root));

            var bookDir = Path.Combine(root, "content", "series", "demo", "books", "book-one");
            await Assert.That(ManuscriptWorkspace.TryOpen(bookDir, out var fromBook)).IsTrue();
            await Assert.That(fromBook!.ContentRoot).IsEqualTo(Path.GetFullPath(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string CreateWorkspace()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-core-{Guid.NewGuid():N}");
        var series = Path.Combine(root, "content", "series", "demo");
        var book = Path.Combine(series, "books", "book-one");
        var chapters = Path.Combine(book, "chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(series, "series.yaml"), "id: demo\nname: Demo\n");
        File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Book One\nauthor: Test\n");
        File.WriteAllText(Path.Combine(chapters, "001-alpha.md"), "# Chapter 1 - Alpha\n\nHello.\n");
        File.WriteAllText(Path.Combine(chapters, "002-beta.md"), "# Chapter 2 - Beta\n\nHello.\n");

        var lone = Path.Combine(root, "content", "books", "lone");
        Directory.CreateDirectory(Path.Combine(lone, "chapters"));
        File.WriteAllText(Path.Combine(lone, "book.yaml"), "title: Lone\n");
        File.WriteAllText(Path.Combine(lone, "chapters", "001.md"), "# Chapter 1 - Only\n\nBody.\n");
        return root;
    }
}
