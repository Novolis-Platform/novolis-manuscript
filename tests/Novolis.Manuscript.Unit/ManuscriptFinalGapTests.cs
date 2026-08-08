using Novolis.Manuscript;
using Novolis.Manuscript.Export.Audio;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptFinalGapTests
{
    static readonly byte[] TinyMp3 =
    [
        0xFF, 0xF3, 0x48, 0xC4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    ];

    [Test]
    public async Task Pipeline_force_rebuild_and_synth_failure()
    {
        var temp = Path.Combine(Path.GetTempPath(), $"ms-force-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        try
        {
            var ch = Path.Combine(temp, "ch01.md");
            await File.WriteAllTextAsync(ch, "# One\n\nHello.\n");
            var outDir = Path.Combine(temp, "out");
            var pipeline = new AudiobookPipeline(new CountingSynthesizer());
            var chapters = new[] { new AudiobookChapterInput("ch01", "One", ch) };
            var voice = new VoiceSettings();

            await pipeline.GenerateAsync("book", chapters, voice, new AudiobookOptions
            {
                OutputDirectory = outDir,
                AssembleMode = AudiobookAssembleMode.None,
            });

            var forced = new CountingSynthesizer();
            var pipeline2 = new AudiobookPipeline(forced);
            await pipeline2.GenerateAsync("book", chapters, voice, new AudiobookOptions
            {
                OutputDirectory = outDir,
                AssembleMode = AudiobookAssembleMode.None,
                Force = true,
            });
            await Assert.That(forced.Calls).IsGreaterThan(0);

            var failing = new AudiobookPipeline(new FailingSynthesizer());
            await Assert.That(async () => await failing.GenerateAsync(
                    "book",
                    chapters,
                    voice,
                    new AudiobookOptions { OutputDirectory = Path.Combine(temp, "fail") }))
                .ThrowsExactly<InvalidOperationException>();
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Test]
    public async Task Doctor_missing_file_and_missing_book_yaml()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-doc2-{Guid.NewGuid():N}");
        try
        {
            var bookDir = Path.Combine(root, "content", "books", "broken");
            Directory.CreateDirectory(Path.Combine(bookDir, "chapters"));
            File.WriteAllText(Path.Combine(bookDir, "chapters", "01.md"), "# Chapter 1 - X\n\nHi.\n");
            // no book.yaml
            var findings = ManuscriptDoctor.Diagnose(root);
            await Assert.That(findings.Any(f => f.Code == "missing-book-yaml")).IsTrue();

            var missingPath = Path.Combine(bookDir, "chapters", "gone.md");
            var book = new BookInfo(
                "broken",
                "Broken",
                null,
                null,
                bookDir,
                null,
                [new ChapterInfo("gone", "Gone", ChapterKind.Chapter, 1, missingPath)],
                false,
                false,
                []);
            var bookFindings = ManuscriptDoctor.Diagnose(book);
            await Assert.That(bookFindings.Any(f => f.Code == "missing-chapter-file")).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Ascii_noop_file_and_relax_write()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ms-ascii2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var clean = Path.Combine(dir, "clean.md");
            File.WriteAllText(clean, "plain ascii\n");
            var result = ManuscriptAscii.NormalizeFile(clean, dryRun: false, relax: false);
            await Assert.That(result.Replacements).IsEqualTo(0);

            var dirty = Path.Combine(dir, "dirty.md");
            File.WriteAllText(dirty, "snowman \u2603\n");
            var relaxed = ManuscriptAscii.NormalizeFile(dirty, dryRun: false, relax: true);
            await Assert.That(relaxed.HasRemainingNonAscii).IsTrue();
            // relax true with remaining non-ascii still writes when replacements happened? snowman isn't replaced so replacements==0
            await Assert.That(File.ReadAllText(dirty)).Contains('\u2603');
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Doctor_valid_nmp_and_no_chapters()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-doc-nmp-{Guid.NewGuid():N}");
        try
        {
            var book = Path.Combine(root, "src", "Fiction", "u1", "s1", "b1");
            Directory.CreateDirectory(Path.Combine(book, "Chapters"));
            File.WriteAllText(Path.Combine(root, "manuscript.yaml"), """
                protocol: novolis.manuscript
                version: 1
                """);
            File.WriteAllText(Path.Combine(root, "src", "Fiction", "u1", "universe.yaml"), "title: Uni\n");
            File.WriteAllText(Path.Combine(root, "src", "Fiction", "u1", "s1", "series.yaml"), "title: Series\n");
            File.WriteAllText(Path.Combine(book, "book.yaml"), """
                title: Book
                order: 1
                debug_mode: true
                """);
            File.WriteAllText(Path.Combine(book, "Chapters", "1-one.md"), "# Chapter 1 - One\n\nHi.\n");

            var findings = ManuscriptDoctor.Diagnose(root);
            await Assert.That(findings.Count).IsGreaterThan(0);
            await Assert.That(findings.Any(f =>
                f.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning or DiagnosticSeverity.Info)).IsTrue();

            var emptyFindings = ManuscriptDoctor.Diagnose(new BookInfo(
                "empty",
                "Empty",
                null,
                null,
                book,
                "s1",
                [],
                false,
                false,
                []));
            await Assert.That(emptyFindings.Any(f => f.Code == "no-chapters")).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Catalog_find_miss_and_order_coercion()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-ord-{Guid.NewGuid():N}");
        try
        {
            await Assert.That(() => new ManuscriptCatalog().Load(Path.Combine(root, "missing")))
                .ThrowsExactly<DirectoryNotFoundException>();

            var series = Path.Combine(root, "src", "Fiction", "u1", "cycle");
            Directory.CreateDirectory(Path.Combine(series, "a", "Chapters"));
            Directory.CreateDirectory(Path.Combine(series, "b", "Chapters"));
            File.WriteAllText(Path.Combine(root, "manuscript.yaml"), """
                protocol: novolis.manuscript
                version: 1
                """);
            File.WriteAllText(Path.Combine(root, "src", "Fiction", "u1", "universe.yaml"), "title: U\n");
            File.WriteAllText(Path.Combine(series, "series.yaml"), "title: Cycle\n");
            File.WriteAllText(Path.Combine(series, "a", "book.yaml"), "title: A\norder: 2\n");
            File.WriteAllText(Path.Combine(series, "a", "Chapters", "1.md"), "# One\n\n");
            File.WriteAllText(Path.Combine(series, "b", "book.yaml"), "title: B\norder: \"1\"\nauthors: SoloAuthor\n");
            File.WriteAllText(Path.Combine(series, "b", "Chapters", "1.md"), "# One\n\n");

            var catalog = new ManuscriptCatalog();
            var loaded = catalog.Load(root);
            await Assert.That(loaded.Single().Books[0].Id).IsEqualTo("b");
            await Assert.That(loaded.Single().Books[0].Author).IsEqualTo("SoloAuthor");
            await Assert.That(catalog.FindBook(root, "nope", "x")).IsNull();
            await Assert.That(catalog.FindBook(root, null, "missing-id")).IsNull();
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    sealed class CountingSynthesizer : ISynthesizer
    {
        public int Calls { get; private set; }

        public Task<byte[]> SynthesizeToMp3Async(string text, VoiceSettings settings, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(TinyMp3);
        }

        public Task SaveMp3Async(string text, string path, VoiceSettings settings, CancellationToken cancellationToken = default)
        {
            Calls++;
            return File.WriteAllBytesAsync(path, TinyMp3, cancellationToken);
        }
    }

    sealed class FailingSynthesizer : ISynthesizer
    {
        public Task<byte[]> SynthesizeToMp3Async(string text, VoiceSettings settings, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("synth failed");

        public Task SaveMp3Async(string text, string path, VoiceSettings settings, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("synth failed");
    }
}
