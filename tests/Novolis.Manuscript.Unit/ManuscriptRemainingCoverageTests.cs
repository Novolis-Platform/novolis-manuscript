using Novolis.Manuscript;
using Novolis.Manuscript.Export.Pdf;
using Novolis.Manuscript.LegacyBooks;
using Novolis.Manuscript.Metrics;
using Novolis.Manuscript.Protocol;
using ProtocolWorkspace = Novolis.Manuscript.Protocol.ManuscriptWorkspace;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptRemainingCoverageTests
{
    [Test]
    public async Task Catalog_protocol_standalone_authors_refs_and_load_book_directory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-cat2-{Guid.NewGuid():N}");
        try
        {
            // Protocol standalone fiction book (no series.yaml under universe child)
            var fictionBook = Path.Combine(root, "src", "Fiction", "u1", "standalone-book");
            Directory.CreateDirectory(Path.Combine(fictionBook, "Chapters"));
            Directory.CreateDirectory(Path.Combine(fictionBook, "Appendices"));
            Directory.CreateDirectory(Path.Combine(fictionBook, "References", "ships"));
            Directory.CreateDirectory(Path.Combine(fictionBook, "References", "_archive"));
            File.WriteAllText(Path.Combine(root, "manuscript.yaml"), """
                protocol: novolis.manuscript
                version: 1
                """);
            File.WriteAllText(Path.Combine(root, "src", "Fiction", "u1", "universe.yaml"), "title: U\n");
            File.WriteAllText(Path.Combine(fictionBook, "book.yaml"), """
                title: Standalone
                subtitle: Sub
                authors:
                  - First Author
                  - Second
                order: 3
                debug_mode: true
                """);
            File.WriteAllText(Path.Combine(fictionBook, "Chapters", "1-one.md"), "# Chapter 1 - One\n\nBody.\n");
            File.WriteAllText(Path.Combine(fictionBook, "Appendices", "a-notes.md"), "# Notes\n\nAppendix.\n");
            File.WriteAllText(Path.Combine(fictionBook, "References", "ships", "calypso.md"), "# Calypso\n\nShip.\n");
            File.WriteAllText(Path.Combine(fictionBook, "References", "_archive", "old.md"), "# Old\n\n");
            File.WriteAllText(Path.Combine(fictionBook, "References", "flat.md"), "# Flat\n\n");

            // NonFiction subject book
            var nf = Path.Combine(root, "src", "NonFiction", "prog", "intro");
            Directory.CreateDirectory(Path.Combine(nf, "Chapters"));
            File.WriteAllText(Path.Combine(root, "src", "NonFiction", "prog", "subject.yaml"), "title: Programming\n");
            File.WriteAllText(Path.Combine(nf, "book.yaml"), "title: Intro\nauthor: Solo\n");
            File.WriteAllText(Path.Combine(nf, "Chapters", "1-start.md"), "# Start\n\nHi.\n");

            // Flat references only (no section dirs) on a series
            var series = Path.Combine(root, "src", "Fiction", "u1", "the-cycle");
            Directory.CreateDirectory(Path.Combine(series, "book-a", "Chapters"));
            Directory.CreateDirectory(Path.Combine(series, "references"));
            File.WriteAllText(Path.Combine(series, "series.yaml"), "title: Cycle\n");
            File.WriteAllText(Path.Combine(series, "book-a", "book.yaml"), "title: A\norder: 1\n");
            File.WriteAllText(Path.Combine(series, "book-a", "Chapters", "1.md"), "# Chapter 1 - A\n\n");
            File.WriteAllText(Path.Combine(series, "references", "glossary.md"), "# Glossary\n\n");

            var catalog = new ManuscriptCatalog();
            var seriesList = catalog.Load(root);
            await Assert.That(seriesList.Count).IsGreaterThan(0);
            var standalone = catalog.LoadStandaloneBooks(root);
            await Assert.That(standalone.Any(b => b.Id == "standalone-book")).IsTrue();
            await Assert.That(standalone.Any(b => b.Id == "intro")).IsTrue();
            var stand = standalone.First(b => b.Id == "standalone-book");
            await Assert.That(stand.Author).IsEqualTo("First Author");
            await Assert.That(stand.DebugMode).IsTrue();
            await Assert.That(stand.Chapters.Any(c => c.Kind == ChapterKind.Appendix)).IsTrue();
            await Assert.That(stand.References.Any(r => r.Id == "ships")).IsTrue();
            await Assert.That(stand.References.SelectMany(r => r.Files).Any(f => f.Id == "old")).IsFalse();

            var loaded = ManuscriptCatalog.LoadBookDirectory(fictionBook, seriesId: null, protocolLayout: true);
            await Assert.That(loaded.Title).IsEqualTo("Standalone");

            var cycle = seriesList.First(s => s.Id == "the-cycle");
            await Assert.That(cycle.References.Any(r => r.Id == "references")).IsTrue();

            await Assert.That(ChapterOrder.GetFilenameSortKey("00-frontmatter.md")).IsEqualTo(-1);
            await Assert.That(ChapterOrder.GetFilenameSortKey("1.5-interlude.md")).IsEqualTo(1.5);
            await Assert.That(double.IsPositiveInfinity(ChapterOrder.GetFilenameSortKey("prologue.md"))).IsTrue();

            var tmp = Path.Combine(root, "sort-key.md");
            File.WriteAllText(tmp, "\uFEFF<!-- booktools-chapter: 7 -->\n# Chapter 7 - X\n");
            await Assert.That(ChapterOrder.GetSortKey(tmp)).IsEqualTo(7);
            File.WriteAllText(tmp, "---\nchapter: 8.5\n---\n# Title\n");
            await Assert.That(ChapterOrder.GetSortKey(tmp)).IsEqualTo(8.5);
            File.WriteAllText(tmp, "# Just a title\n");
            await Assert.That(ChapterOrder.ReadChapterTitle(tmp)).IsEqualTo("Just a title");
            File.WriteAllText(Path.Combine(root, "00-frontmatter.md"), "# Front\n");
            await Assert.That(ChapterOrder.GetSortKey(Path.Combine(root, "00-frontmatter.md"))).IsEqualTo(-1);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Protocol_chapter_yaml_lists_and_targets()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-proto2-{Guid.NewGuid():N}");
        try
        {
            var book = Path.Combine(root, "src", "Fiction", "u1", "s1", "b1");
            var chapters = Path.Combine(book, "Chapters");
            Directory.CreateDirectory(chapters);
            File.WriteAllText(Path.Combine(root, "manuscript.yaml"), """
                protocol: novolis.manuscript
                version: 1
                """);
            File.WriteAllText(Path.Combine(root, "src", "Fiction", "u1", "universe.yaml"), "title: Uni\n");
            File.WriteAllText(Path.Combine(root, "src", "Fiction", "u1", "s1", "series.yaml"), "title: Series\n");
            File.WriteAllText(Path.Combine(book, "book.yaml"), """
                title: Book
                order: 1
                targets:
                  words: 12000
                authors:
                  - A
                  - B
                """);
            File.WriteAllText(Path.Combine(chapters, "1-opening.md"), """
                ---
                status: draft
                tags: alpha, beta
                date: 2026-01-01
                time: "08:00"
                system: Sol
                location: Bridge
                locations:
                  - Bridge
                  - Hangar
                pov: Ryn
                characters:
                  - Ryn
                  - Tess
                ---

                # Chapter 1 - Opening

                Body with enough text.
                """);
            File.WriteAllText(Path.Combine(chapters, "2-comma.md"), """
                ---
                tags: one
                characters: Ryn, Tess, Kai
                ---

                # Chapter 2 - Comma

                More body.
                """);
            File.WriteAllText(Path.Combine(chapters, "00-frontmatter.md"), """
                ---
                status: outline
                ---

                # Front Matter

                Intro.
                """);
            var appendices = Path.Combine(book, "Appendices");
            Directory.CreateDirectory(appendices);
            File.WriteAllText(Path.Combine(appendices, "appendix-a-notes.md"), """
                # Appendix Notes

                Extra.
                """);

            var refs = Path.Combine(root, "src", "Fiction", "u1", "s1", "References", "ships");
            Directory.CreateDirectory(refs);
            File.WriteAllText(Path.Combine(refs, "calypso.md"), """
                ---
                aliases:
                  - Cal
                tags: ship, capital
                ---

                # Calypso

                Notes.
                """);

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Catalog.Fiction.Count).IsGreaterThan(0);
            var chapter = snapshot.Catalog.Fiction[0].Series[0].Books[0].Chapters
                .First(d => d.Slug.Contains("opening", StringComparison.OrdinalIgnoreCase));
            await Assert.That(chapter.Metadata.Tags).IsNotNull();
            await Assert.That(chapter.Metadata.Tags!.Count).IsGreaterThan(0);
            await Assert.That(chapter.Metadata.Characters).IsNotNull();
            await Assert.That(chapter.Metadata.Characters!.Count).IsGreaterThan(0);
            await Assert.That(snapshot.Diagnostics).IsNotNull();

            // Error paths: missing book title, invalid chapter front matter, broken reference FM
            File.WriteAllText(Path.Combine(book, "book.yaml"), "order: 1\n");
            File.WriteAllText(Path.Combine(chapters, "3-badfm.md"), """
                ---
                - not
                - a
                - mapping
                ---

                # Chapter 3 - Bad

                Body.
                """);
            File.WriteAllText(Path.Combine(refs, "broken.md"), """
                ---
                - list
                ---

                # Broken

                """);
            var errors = ProtocolWorkspace.Open(root).Read();
            await Assert.That(errors.Diagnostics.Any(d =>
                d.Code is ManuscriptDiagnosticCodes.MissingBookTitle
                    or ManuscriptDiagnosticCodes.InvalidYaml)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Pdf_time_before_date_and_print_settings_save()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-pdf2-{Guid.NewGuid():N}");
        var outDir = Path.Combine(root, "out");
        try
        {
            var book = Path.Combine(root, "content", "books", "demo");
            Directory.CreateDirectory(Path.Combine(book, "chapters"));
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Demo\ndebug_mode: true\n");
            File.WriteAllText(Path.Combine(book, "chapters", "01.md"), """
                # Chapter 1 - Meta

                > [!time] 09:15 [!date] 2026-02-02 [!system] Sol

                Body.
                """);
            var settings = new ManuscriptPrintSettings { BodyFontSize = 10, IncludeCover = false };
            var settingsPath = Path.Combine(root, "print.json");
            settings.Save(settingsPath);
            await Assert.That(File.Exists(settingsPath)).IsTrue();
            var loaded = ManuscriptPrintSettings.Load(settingsPath);
            await Assert.That(loaded.BodyFontSize).IsEqualTo(10f);
            var pdfOpts = loaded.ToPdfOptions("T", "S", "A");
            await Assert.That(pdfOpts.Title).IsEqualTo("T");

            var paths = BookPrintExporter.ExportBookFolder(
                book,
                outDir,
                "",
                "demo",
                new BookPrintOptions { ShowAllMetadataTags = true, PrintSettingsPath = settingsPath });
            var txt = await File.ReadAllTextAsync(paths.TextPath);
            await Assert.That(txt).Contains("2026-02-02");
            await Assert.That(txt).Contains("09:15");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task LegacyBooks_covers_booktools_yaml_and_callouts()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-leg2-{Guid.NewGuid():N}");
        try
        {
            var series = Path.Combine(root, "content", "series", "demo");
            var book = Path.Combine(series, "books", "one");
            Directory.CreateDirectory(Path.Combine(book, "chapters"));
            Directory.CreateDirectory(Path.Combine(series, "references", "ships"));
            File.WriteAllText(Path.Combine(series, "series.yaml"), "id: demo\nname: Demo\n");
            File.WriteAllText(Path.Combine(book, "book.yaml"), """
                title: One
                authors: [A, B]
                targets:
                  words: 9000
                """);
            File.WriteAllText(Path.Combine(book, "chapters", "01-alpha.md"), """
                <!-- booktools-chapter: 1 -->

                # Chapter 1 - Alpha

                > [!pov] Ryn
                > [!characters] Ryn, Tess

                Body.
                """);
            File.WriteAllText(Path.Combine(book, "chapters", "02-beta.md"), """
                ---
                chapter: 2
                ---

                # Chapter 2 - Beta

                Body.
                """);
            File.WriteAllText(Path.Combine(series, "references", "ships", "calypso.md"), "# Calypso\n\n");
            var lone = Path.Combine(root, "content", "books", "lone");
            Directory.CreateDirectory(Path.Combine(lone, "chapters"));
            File.WriteAllText(Path.Combine(lone, "book.yaml"), "title: Lone\n");
            File.WriteAllText(Path.Combine(lone, "chapters", "1.md"), "# Only\n\n");

            var snapshot = new LegacyBooksCatalogReader().Read(root);
            await Assert.That(snapshot.Catalog.Fiction[0].Series.Count).IsGreaterThan(0);
            await Assert.That(snapshot.Catalog.NonFiction[0].Books.Count).IsGreaterThan(0);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Metrics_target_words_string_and_coerce_branches()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-met2-{Guid.NewGuid():N}");
        try
        {
            var book = Path.Combine(root, "content", "books", "x");
            Directory.CreateDirectory(Path.Combine(book, "chapters"));
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: X\ntarget_words: \"2500\"\n");
            File.WriteAllText(Path.Combine(book, "chapters", "1.md"), "# Chapter 1 - X\n\nWords here.\n");
            var dto = ManuscriptMetrics.ComputeOne(root, "", "x");
            await Assert.That(dto.TargetWords).IsEqualTo(2500);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
