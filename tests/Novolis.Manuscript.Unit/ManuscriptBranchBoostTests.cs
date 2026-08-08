using System.Text;
using Markdig;
using Novolis.Manuscript;
using Novolis.Manuscript.Export.Audio;
using Novolis.Manuscript.Export.Pdf;
using Novolis.Manuscript.LegacyBooks;
using Novolis.Manuscript.Metrics;
using Novolis.Manuscript.Protocol;
using Novolis.Manuscript.Protocol.Internal;
using ProtocolWorkspace = Novolis.Manuscript.Protocol.ManuscriptWorkspace;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptBranchBoostTests
{
    [Test]
    public async Task Protocol_catalog_error_paths_series_book_docs_and_refs()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            // Book directly under universe (not via series)
            var directBook = Path.Combine(root, "src", "Fiction", "u1", "solo");
            Directory.CreateDirectory(Path.Combine(directBook, "Chapters"));
            File.WriteAllText(Path.Combine(directBook, "book.yaml"), "title: Solo\n");
            File.WriteAllText(
                Path.Combine(directBook, "Chapters", "1-ok.md"),
                "\uFEFF---\ntags: a, b\nlocations:\n  - Bridge\ncharacters:\n  - Ryn\n---\n# Solo\n\nBody.\n");
            File.WriteAllText(Path.Combine(directBook, "Chapters", "frontmatter.md"), "# Front\n\nIntro.\n");
            File.WriteAllText(Path.Combine(directBook, "Chapters", "00-frontmatter.md"), "# Also Front\n\n");
            File.WriteAllText(Path.Combine(directBook, "Chapters", "bad name.md"), "# Bad\n\n");
            File.WriteAllText(Path.Combine(directBook, "Chapters", "2-dup.md"), "# Dup A\n\n");
            File.WriteAllText(Path.Combine(directBook, "Chapters", "2-other.md"), "# Dup B\n\n");

            var appendices = Path.Combine(directBook, "Appendices");
            Directory.CreateDirectory(appendices);
            File.WriteAllText(Path.Combine(appendices, "appendix-a-notes.md"), "# Notes\n\nApp.\n");
            File.WriteAllText(Path.Combine(appendices, "not-appendix.md"), "# Bad appendix name\n\n");

            // Series missing yaml / invalid id / failed series meta
            var badSeries = Path.Combine(root, "src", "Fiction", "u1", "Bad_Series");
            Directory.CreateDirectory(badSeries);
            File.WriteAllText(Path.Combine(badSeries, "series.yaml"), "title: Bad\n");

            var missingSeriesYaml = Path.Combine(root, "src", "Fiction", "u1", "orphan-dir");
            Directory.CreateDirectory(missingSeriesYaml);
            // neither series.yaml nor book.yaml → skipped (no MissingSeriesMetadata; that arm is unreachable via ReadUniverse)

            var brokenSeries = Path.Combine(root, "src", "Fiction", "u1", "cycle2");
            Directory.CreateDirectory(Path.Combine(brokenSeries, "book-x", "Chapters"));
            File.WriteAllText(Path.Combine(brokenSeries, "series.yaml"), "description: no title\n");
            File.WriteAllText(Path.Combine(brokenSeries, "book-x", "book.yaml"), "title: X\norder: 1\n");

            var goodSeries = Path.Combine(root, "src", "Fiction", "u1", "cycle3");
            var seriesBook = Path.Combine(goodSeries, "book-a");
            Directory.CreateDirectory(Path.Combine(seriesBook, "Chapters"));
            File.WriteAllText(Path.Combine(goodSeries, "series.yaml"), "title: Cycle3\n");
            File.WriteAllText(Path.Combine(seriesBook, "book.yaml"), "title: A\norder: 1\n");
            File.WriteAllText(Path.Combine(seriesBook, "Chapters", "1-a.md"), "# A\n\nBody.\n");
            // empty References folder under series
            Directory.CreateDirectory(Path.Combine(goodSeries, "References"));

            // Non-fiction subject with book + empty refs
            var subject = Path.Combine(root, "src", "NonFiction", "craft");
            var nfBook = Path.Combine(subject, "guide");
            Directory.CreateDirectory(Path.Combine(nfBook, "Chapters"));
            Directory.CreateDirectory(Path.Combine(subject, "References"));
            File.WriteAllText(Path.Combine(subject, "subject.yaml"), "title: Craft\n");
            File.WriteAllText(Path.Combine(nfBook, "book.yaml"), """
                title: Guide
                subtitle: Sub
                authors: [A]
                language: en
                description: D
                rights: R
                targets:
                  words: 1200
                publication:
                  version: "1.0"
                  isbn: "978"
                  date: "2026-01-01"
                defaults:
                  language: en
                unknown_field: x
                """);
            File.WriteAllText(Path.Combine(nfBook, "Chapters", "1-g.md"), "# Guide\n\nBody.\n");

            // Broken universe meta under sibling
            var brokenUniverse = Path.Combine(root, "src", "Fiction", "u2");
            Directory.CreateDirectory(brokenUniverse);
            File.WriteAllText(Path.Combine(brokenUniverse, "universe.yaml"), "description: missing title\n");

            // Empty References at universe (warning already covered elsewhere) + nested ref with BOM
            var refs = Path.Combine(root, "src", "Fiction", "u1", "References", "ships");
            Directory.CreateDirectory(refs);
            File.WriteAllText(Path.Combine(refs, "calypso.md"), "\uFEFF---\naliases: [tramp]\ntags: [ship]\n---\n# Calypso\n\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Catalog.Fiction.Count).IsGreaterThan(0);
            await Assert.That(snapshot.Diagnostics.Count).IsGreaterThan(5);
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.InvalidDocumentFilename)).IsTrue();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.DuplicateDocumentOrder)).IsTrue();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.EmptyReferenceFolder)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task ProtocolMetadataReader_nested_unknown_and_chapter_coercion()
    {
        var reader = new ProtocolMetadataReader();
        var diagnostics = new List<ManuscriptDiagnostic>();
        var root = Path.Combine(Path.GetTempPath(), $"ms-meta-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var ws = Path.Combine(root, "manuscript.yaml");
            File.WriteAllText(ws, """
                protocol: novolis.manuscript
                version: 1
                defaults:
                  authors: [W]
                  language: en
                  rights: R
                  weird: 1
                """);
            var workspace = reader.ReadWorkspace(ws, diagnostics);
            await Assert.That(workspace.Success).IsTrue();
            await Assert.That(diagnostics.Any(d => d.Code == ManuscriptDiagnosticCodes.UnknownMetadataField)).IsTrue();

            var bookPath = Path.Combine(root, "book.yaml");
            File.WriteAllText(bookPath, """
                title: T
                targets: not-a-map
                publication:
                  version: "1"
                  mystery: yes
                """);
            var book = reader.ReadBook(bookPath, diagnostics);
            // Non-mapping targets may fail DTO deserialize (YamlException → FailYaml).
            await Assert.That(book.Success || diagnostics.Any(d => d.Code == ManuscriptDiagnosticCodes.InvalidYaml)).IsTrue();

            File.WriteAllText(bookPath, """
                title: T2
                publication:
                  version: "1"
                  mystery: yes
                """);
            var book2 = reader.ReadBook(bookPath, diagnostics);
            await Assert.That(book2.Success).IsTrue();
            await Assert.That(diagnostics.Any(d => d.Code == ManuscriptDiagnosticCodes.UnknownMetadataField)).IsTrue();

            var chapterDiag = new List<ManuscriptDiagnostic>();
            var emptyTags = reader.ReadChapterFrontMatter("tags: \",,\"\nlocation: Bridge\ndate: 1\nsystem: 2\n", "c.md", chapterDiag);
            await Assert.That(emptyTags.Success).IsTrue();

            var listLoc = reader.ReadChapterFrontMatter(
                "locations: []\ncharacters: []\ntags: []\n",
                "c2.md",
                chapterDiag);
            await Assert.That(listLoc.Success).IsTrue();

            var scalarLoc = reader.ReadChapterFrontMatter(
                "location: Bridge\ncharacters: Ryn, Tess\ntags: alpha\n",
                "c3.md",
                chapterDiag);
            await Assert.That(scalarLoc.Success).IsTrue();
            await Assert.That(scalarLoc.Value!.Locations![0]).IsEqualTo("Bridge");

            var unknownChapter = reader.ReadChapterFrontMatter("bogus: 1\npov: Ryn\n", "c4.md", chapterDiag);
            await Assert.That(unknownChapter.Success).IsTrue();
            await Assert.That(chapterDiag.Any(d => d.Code == ManuscriptDiagnosticCodes.UnknownMetadataField)).IsTrue();

            var entity = Path.Combine(root, "series.yaml");
            File.WriteAllText(entity, """
                title: S
                defaults:
                  authors: [A]
                  language: en
                  rights: R
                """);
            var series = reader.ReadSeries(entity, diagnostics);
            await Assert.That(series.Success).IsTrue();

            // Malformed YAML that YamlStream rejects
            File.WriteAllText(bookPath, "title: [\n");
            var bad = reader.ReadBook(bookPath, diagnostics);
            await Assert.That(bad.Success).IsFalse();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task ReferenceReader_path_escape_and_empty_folder()
    {
        var reader = new ReferenceReader(new ProtocolMetadataReader());
        var diagnostics = new List<ManuscriptDiagnostic>();
        var root = Path.Combine(Path.GetTempPath(), $"ms-refread-{Guid.NewGuid():N}");
        var refs = Path.Combine(root, "References");
        Directory.CreateDirectory(refs);
        try
        {
            var empty = reader.Read(refs, "scope", root, diagnostics);
            await Assert.That(empty).IsEmpty();
            await Assert.That(diagnostics.Any(d => d.Code == ManuscriptDiagnosticCodes.EmptyReferenceFolder)).IsTrue();

            File.WriteAllText(Path.Combine(refs, "ok.md"), "# Ok\n\n");
            // Point workspace root elsewhere so the absolute path is "outside"
            var outsideRoot = Path.Combine(Path.GetTempPath(), $"ms-out-{Guid.NewGuid():N}");
            Directory.CreateDirectory(outsideRoot);
            try
            {
                var escaped = reader.Read(refs, "scope", outsideRoot, diagnostics);
                await Assert.That(escaped).IsEmpty();
                await Assert.That(diagnostics.Any(d => d.Code == ManuscriptDiagnosticCodes.PathEscapesWorkspace)).IsTrue();
            }
            finally
            {
                Directory.Delete(outsideRoot, recursive: true);
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task DocumentReader_direct_covers_frontmatter_appendix_and_bom()
    {
        var reader = new DocumentReader(new ProtocolMetadataReader());
        var diagnostics = new List<ManuscriptDiagnostic>();
        var dir = Path.Combine(Path.GetTempPath(), $"ms-docs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "frontmatter.md"), "\uFEFF# Front\n\nHi\n");
            File.WriteAllText(Path.Combine(dir, "1-a.md"), "# A\n\n");
            File.WriteAllText(Path.Combine(dir, "1-b.md"), "# B\n\n");
            var chapters = reader.ReadDocuments(dir, ManuscriptDocumentKind.Chapter, diagnostics);
            await Assert.That(chapters.Any(d => d.Order == 0)).IsTrue();
            await Assert.That(diagnostics.Any(d => d.Code == ManuscriptDiagnosticCodes.DuplicateDocumentOrder)).IsTrue();

            var app = Path.Combine(Path.GetTempPath(), $"ms-app-{Guid.NewGuid():N}");
            Directory.CreateDirectory(app);
            try
            {
                File.WriteAllText(Path.Combine(app, "appendix-c-extra.md"), "# Extra\n\n");
                File.WriteAllText(Path.Combine(app, "weird.md"), "# Weird\n\n");
                var apps = reader.ReadDocuments(app, ManuscriptDocumentKind.Appendix, diagnostics);
                await Assert.That(apps.Count).IsEqualTo(1);
                await Assert.That(diagnostics.Any(d =>
                    d.Message.Contains("appendix-", StringComparison.Ordinal))).IsTrue();
            }
            finally
            {
                Directory.Delete(app, recursive: true);
            }

            await Assert.That(reader.ReadDocuments(Path.Combine(dir, "missing"), ManuscriptDocumentKind.Chapter, diagnostics))
                .IsEmpty();
            await Assert.That(DocumentReader.ReadFirstH1("<!-- open -->\nplain\n")).IsNull();
            await Assert.That(DocumentReader.SplitFrontMatter("---\nx: 1").FrontMatter).IsNull();
            await Assert.That(DocumentReader.SplitFrontMatter("---\nx: 1\n---").Body).IsEqualTo(string.Empty);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task LegacyBooks_sort_keys_bool_lists_bom_and_reference_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-leg3-{Guid.NewGuid():N}");
        try
        {
            var series = Path.Combine(root, "content", "series", "demo");
            var book = Path.Combine(series, "books", "one");
            var chapters = Path.Combine(book, "chapters");
            Directory.CreateDirectory(chapters);
            Directory.CreateDirectory(Path.Combine(book, "appendices"));
            Directory.CreateDirectory(Path.Combine(series, "reference", "ships")); // singular folder
            File.WriteAllText(Path.Combine(series, "series.yaml"), "id: demo\ntitle: Demo\n");
            File.WriteAllText(Path.Combine(book, "book.yaml"), """
                title: One
                author: Solo
                chapter_order_from_heading: true
                language: en
                description: D
                rights: R
                subtitle: Sub
                """);
            File.WriteAllText(Path.Combine(chapters, "03-gamma.md"), """
                ---
                status: draft
                tags:
                  - a
                  - b
                date: 2026-01-01
                location: Bridge
                characters: Ryn
                chapter: 3
                ---

                # Gamma Title

                Body.
                """);
            File.WriteAllText(Path.Combine(chapters, "plain.md"), """
                # Plain Heading Only

                > [!loc] Hold
                > [!chars] Tess
                > [!point_of_view] Tess
                > [!status] wip
                > [!date] 2026-02-02
                > [!time] 10:00
                > [!system] Sol

                Words.
                """);
            File.WriteAllText(Path.Combine(chapters, "infty.md"), "# No Number Here\n\n");
            File.WriteAllText(Path.Combine(book, "appendices", "a.md"), "# Appendix A\n\n");
            File.WriteAllText(Path.Combine(series, "reference", "ships", "calypso.md"), "# Calypso\n\n");

            // Invalid yaml map + missing yaml
            File.WriteAllText(Path.Combine(book, "book.yaml"), """
                title: One
                author: Solo
                chapter_order_from_heading: "true"
                flags: [a, b]
                """);
            // rewrite after first write above - overwrite
            File.WriteAllText(Path.Combine(book, "book.yaml"), """
                title: One
                author: Solo
                chapter_order_from_heading: "true"
                """);

            var snapshot = new LegacyBooksCatalogReader().Read(root);
            var chapterDocs = snapshot.Catalog.Fiction[0].Series[0].Books[0].Chapters;
            await Assert.That(chapterDocs.Count).IsGreaterThan(2);
            await Assert.That(snapshot.Catalog.Fiction[0].Series[0].References.Count).IsGreaterThan(0);

            // corrupt yaml recovers empty map
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: [\n");
            var again = new LegacyBooksCatalogReader().Read(root);
            await Assert.That(again.Catalog.Fiction[0].Series[0].Books[0].Metadata.Title).IsEqualTo("one");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task ManuscriptMetadata_apply_aliases_and_body_paths()
    {
        var emptyWords = ManuscriptMetadata.CountWords("   \n");
        await Assert.That(emptyWords).IsEqualTo(0);

        var body = ManuscriptMetadata.GetBodyForWordCount("""
            # Chapter 1 - Title

            > [!date] 2026-01-01
            > [!pov] Ryn

            Real words here.
            """);
        await Assert.That(body).Contains("Real words");

        var withAliases = ManuscriptMetadata.Parse("""
            # Chapter 2 - Next

            > [!loc] Bridge
            > [!chars] Ryn
            > [!point_of_view] Tess
            > [!note] aside
            > [!custom] value

            Body.
            """);
        await Assert.That(withAliases.Meta.Location).IsEqualTo("Bridge");
        await Assert.That(withAliases.Meta.Characters).IsEqualTo("Ryn");
        await Assert.That(withAliases.Meta.Pov).IsEqualTo("Tess");
        await Assert.That(withAliases.Meta.Notes).IsEqualTo("aside");
        await Assert.That(withAliases.Meta.Extra["custom"]).IsEqualTo("value");

        var inserted = ManuscriptMetadata.ApplyCallouts("Just body.\n", new ManuscriptChapterMetadata
        {
            Number = "3",
            Title = "Inserted",
            Date = "2026-03-03",
        });
        await Assert.That(inserted).Contains("# Chapter 3 - Inserted");
        await Assert.That(inserted).Contains("[!date]");

        var rewritten = ManuscriptMetadata.ApplyCallouts("""
            # Chapter 1 - Old

            > [!date] old

            Body.
            """, new ManuscriptChapterMetadata
        {
            Number = "1",
            Title = "New",
            Time = "09:00",
        });
        await Assert.That(rewritten).Contains("# Chapter 1 - New");
        await Assert.That(rewritten).Contains("[!time]");
    }

    [Test]
    public async Task ManuscriptAscii_typography_replacements()
    {
        var input = "\u201Chello\u201D \u2018world\u2019\u2026\u00A0\u200B\u2014";
        var result = ManuscriptAscii.Normalize(input);
        await Assert.That(result.Text).Contains("\"hello\"");
        await Assert.That(result.Text).Contains("'world'");
        await Assert.That(result.Text).Contains("...");
        await Assert.That(result.Replacements).IsGreaterThan(3);

        var root = Path.Combine(Path.GetTempPath(), $"ms-ascii-{Guid.NewGuid():N}");
        var chapters = Path.Combine(root, "chapters");
        Directory.CreateDirectory(chapters);
        try
        {
            File.WriteAllText(Path.Combine(chapters, "1.md"), "plain ascii only\n");
            var norm = ManuscriptAscii.NormalizeFile(Path.Combine(chapters, "1.md"), dryRun: false, relax: false);
            await Assert.That(norm.Replacements).IsEqualTo(0);
            var scan = ManuscriptAscii.ScanChaptersDirectory(chapters, limit: 1);
            await Assert.That(scan.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task ExportPdf_dateline_dialogue_strong_and_table_header_only()
    {
        var markdown = """
            # Chapter 1 - Styles

            > Orbit 7 · Watch

            > "Said the captain from the hatch."

            > **Important** keep this.

            | Only |
            | --- |

            A [link](https://example.com) with `code` and
            a soft
            break.
            """;
        var root = Path.Combine(Path.GetTempPath(), $"ms-pdf2-{Guid.NewGuid():N}");
        var outDir = Path.Combine(root, "out");
        var series = Path.Combine(root, "content", "series", "demo");
        var book = Path.Combine(series, "books", "book-one");
        Directory.CreateDirectory(Path.Combine(book, "chapters"));
        try
        {
            File.WriteAllText(Path.Combine(series, "series.yaml"), "id: demo\nname: Demo\n");
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Book\n");
            File.WriteAllText(Path.Combine(book, "chapters", "001.md"), markdown);

            var paths = BookPrintExporter.ExportBookFolder(book, outDir, "demo", "book-one", new BookPrintOptions
            {
                ShowAllMetadataTags = true,
                DebugMode = true,
            });
            var txt = await File.ReadAllTextAsync(paths.TextPath);
            await Assert.That(txt).Contains("Orbit 7");
            await Assert.That(txt).Contains("Important");
            await Assert.That(File.Exists(paths.PdfPath)).IsTrue();

            // Direct plain-text coverage for null inlines / nested containers
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var doc = Markdown.Parse(markdown, pipeline);
            var sb = new StringBuilder();
            PlainTextRenderer.AppendDocument(doc, sb, showAllTags: true);
            await Assert.That(sb.ToString()).Contains("link");
            await Assert.That(PlainTextRenderer.InlinesToPlain(null)).IsEqualTo("");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Catalog_find_standalone_and_order_coercion()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-cat-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "content", "series"));
            var books = new ManuscriptCatalog().LoadStandaloneBooks(root);
            await Assert.That(books).IsEmpty();

            var lone = Path.Combine(root, "content", "books", "lone");
            Directory.CreateDirectory(Path.Combine(lone, "chapters"));
            File.WriteAllText(Path.Combine(lone, "book.yaml"), "title: Lone\norder: \"7\"\n");
            File.WriteAllText(Path.Combine(lone, "chapters", "1.md"), "# Lone\n\nBody.\n");
            await Assert.That(new ManuscriptCatalog().FindBook(root, null, "lone")!.Title).IsEqualTo("Lone");

            var nmp = CreateMinimalWorkspace();
            try
            {
                var series = Path.Combine(nmp, "src", "Fiction", "u1", "cycle");
                var book = Path.Combine(series, "book-a");
                Directory.CreateDirectory(Path.Combine(book, "Chapters"));
                File.WriteAllText(Path.Combine(series, "series.yaml"), "title: Cycle\nname: Cycle Name\n");
                File.WriteAllText(Path.Combine(book, "book.yaml"), "title: A\norder: 2\ndebug_mode: true\n");
                File.WriteAllText(Path.Combine(book, "Chapters", "1-a.md"), "# A\n\n");
                Directory.CreateDirectory(Path.Combine(book, "Appendices"));
                File.WriteAllText(Path.Combine(book, "Appendices", "appendix-a-x.md"), "# App\n\n");
                File.WriteAllText(Path.Combine(series, "style.css"), "body{}\n");

                var found = new ManuscriptCatalog().FindBook(nmp, "cycle", "book-a");
                await Assert.That(found).IsNotNull();
                await Assert.That(new ManuscriptCatalog().Load(nmp).Count).IsGreaterThan(0);

                var outDir = Path.Combine(nmp, "out");
                var paths = BookPrintExporter.ExportBookFolder(
                    book,
                    outDir,
                    "cycle",
                    "book-a",
                    new BookPrintOptions { SeriesTitle = null });
                await Assert.That(File.Exists(paths.PdfPath)).IsTrue();
                await Assert.That(StylesheetLocator.Find(book, nmp, stopAt: nmp)).IsNotNull();
                await Assert.That(StylesheetLocator.Find(null, Path.Combine(nmp, "style"), stopAt: null)).IsNull();
                Directory.CreateDirectory(Path.Combine(nmp, "style"));
                File.WriteAllText(Path.Combine(nmp, "style", "style.css"), "x{}\n");
                await Assert.That(StylesheetLocator.Find(Path.Combine(nmp, "missing"), nmp)).Contains("style.css");
            }
            finally
            {
                Directory.Delete(nmp, recursive: true);
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Metrics_coerce_int_branches_and_workspace_locator_file()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-met3-{Guid.NewGuid():N}");
        try
        {
            var book = Path.Combine(root, "content", "books", "x");
            Directory.CreateDirectory(Path.Combine(book, "chapters"));
            File.WriteAllText(Path.Combine(book, "book.yaml"), """
                title: X
                targets:
                  words: 3000
                """);
            File.WriteAllText(Path.Combine(book, "chapters", "1.md"), "# Chapter 1 - X\n\nWords here.\n");
            var dto = ManuscriptMetrics.ComputeOne(root, "", "x");
            await Assert.That(dto.TargetWords).IsEqualTo(3000);

            File.WriteAllText(Path.Combine(book, "book.yaml"), """
                title: X
                targets:
                  words: 2500.9
                """);
            dto = ManuscriptMetrics.ComputeOne(root, "", "x");
            await Assert.That(dto.TargetWords).IsEqualTo(2500);

            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: X\ntarget_words: 111\n");
            dto = ManuscriptMetrics.ComputeOne(root, "", "x");
            await Assert.That(dto.TargetWords).IsEqualTo(111);

            var nmp = CreateMinimalWorkspace();
            try
            {
                var marker = Path.Combine(nmp, "manuscript.yaml");
                await Assert.That(ProtocolWorkspace.Open(marker).RootPath).IsEqualTo(Path.GetFullPath(nmp));
                await Assert.That(ProtocolWorkspace.Open(Path.Combine(nmp, "src", "Fiction", "u1")).RootPath)
                    .IsEqualTo(Path.GetFullPath(nmp));
            }
            finally
            {
                Directory.Delete(nmp, recursive: true);
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Audiobook_progress_tracker_phase_branches()
    {
        var reports = new List<AudiobookProgress>();
        var progress = new CollectingProgress(reports);
        var chapters = new[]
        {
            new AudiobookChapterInput("a", "A", "a.md"),
            new AudiobookChapterInput("b", "B", "b.md"),
        };
        var tracker = new AudiobookPipeline.ProgressTracker(chapters, AudiobookAssembleMode.Both, progress);
        tracker.MarkRunning(0);
        tracker.MarkSegment(0, 1, 2);
        tracker.MarkFinished(0, cached: true);
        tracker.MarkFailed(1);
        tracker.ReportAssemblingMp3();
        tracker.ReportAssemblingM4b();
        tracker.ReportWritingManifest();
        tracker.ReportCompleted();
        await Assert.That(reports.Count).IsGreaterThan(5);
        await Assert.That(reports.Any(r => r.Phase == AudiobookProgressPhase.AssemblingM4b)).IsTrue();
        await Assert.That(reports.Any(r => r.Message.Contains("M4B", StringComparison.OrdinalIgnoreCase))).IsTrue();

        var empty = new AudiobookPipeline.ProgressTracker([], AudiobookAssembleMode.None, progress);
        empty.ReportSynthesizing();
        empty.ReportCompleted();
        await Assert.That(reports.Any(r => r.TotalChapters == 0)).IsTrue();
    }

    sealed class CollectingProgress(List<AudiobookProgress> sink) : IProgress<AudiobookProgress>
    {
        public void Report(AudiobookProgress value) => sink.Add(value);
    }

    [Test]
    public async Task BookYaml_and_legacy_bool_list_edges()
    {
        await Assert.That(BookYaml.LoadFile(null)).IsEmpty();
        await Assert.That(BookYaml.GetBool(new Dictionary<string, object?>(), "x", defaultValue: true)).IsTrue();
        await Assert.That(BookYaml.GetBool(new Dictionary<string, object?> { ["x"] = true }, "x")).IsTrue();
        await Assert.That(BookYaml.GetBool(new Dictionary<string, object?> { ["x"] = "false" }, "x")).IsFalse();

        var root = Path.Combine(Path.GetTempPath(), $"ms-leg4-{Guid.NewGuid():N}");
        try
        {
            var series = Path.Combine(root, "content", "series", "demo");
            var book = Path.Combine(series, "books", "one");
            Directory.CreateDirectory(Path.Combine(book, "chapters"));
            File.WriteAllText(Path.Combine(series, "series.yaml"), "id: demo\n");
            File.WriteAllText(Path.Combine(book, "book.yaml"), """
                title: One
                chapter_order_from_heading: false
                """);
            // BOM + booktools + numeric filename sort fallback without heading order
            File.WriteAllText(Path.Combine(book, "chapters", "09-z.md"), "\uFEFF<!-- booktools-chapter: 9 -->\n\n# Zebra\n\n");
            File.WriteAllText(Path.Combine(book, "chapters", "nope.md"), "no heading at all\n");
            var snap = new LegacyBooksCatalogReader().Read(root);
            await Assert.That(snap.Catalog.Fiction[0].Series[0].Books[0].Chapters.Count).IsEqualTo(2);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string CreateMinimalWorkspace(bool writeUniverse = true)
    {
        var root = Path.Combine(Path.GetTempPath(), "nmp-boost-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "manuscript.yaml"), """
            protocol: novolis.manuscript
            version: 1
            """);

        var universe = Path.Combine(root, "src", "Fiction", "u1");
        Directory.CreateDirectory(universe);
        if (writeUniverse)
            File.WriteAllText(Path.Combine(universe, "universe.yaml"), "title: Universe\n");

        return root;
    }
}
