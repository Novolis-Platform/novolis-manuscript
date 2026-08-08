using Novolis.Manuscript;
using Novolis.Manuscript.Editorial;
using Novolis.Manuscript.Export.Markdown;
using Novolis.Manuscript.Export.Pdf;

namespace Novolis.Manuscript.Unit.Export;

public sealed class PrintModelCoverageBoostTests
{
    [Test]
    public async Task Assembler_skips_missing_files_and_synthetic_heading()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"nope-{Guid.NewGuid():N}.md");
        var views = BookPrintAssembler.FromChapterFiles([missing, ""]);
        await Assert.That(views.Count).IsEqualTo(0);

        var noHeading = """
            ---
            date: "1"
            time: "2"
            ---

            Just prose without a heading.
            """;
        var view = BookPrintAssembler.FromChapterMarkdown(noHeading, id: "x");
        await Assert.That(view.Id).IsEqualTo("x");
        await Assert.That(view.BodyMarkdown).Contains("Just prose");
        await Assert.That(view.ReaderDatelineLines[0]).IsEqualTo("1 2");

        var timeFirst = BookPrintAssembler.MergeDateTimeLines(
        [
            ("time", "09:00"),
            ("date", "2495.001"),
            ("system", "X"),
            ("", "skip"),
            ("location", "Here"),
        ]);
        await Assert.That(timeFirst[0]).IsEqualTo("2495.001 09:00");
        await Assert.That(timeFirst).Contains("X");
    }

    [Test]
    public async Task Assembler_from_book_and_empty_id_defaults()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-asm-{Guid.NewGuid():N}");
        var chapters = Path.Combine(root, "chapters");
        Directory.CreateDirectory(chapters);
        var path = Path.Combine(chapters, "10-a.md");
        File.WriteAllText(path, "# Title Only\n\nBody.\n");
        try
        {
            var book = new BookInfo(
                "b1",
                "Book",
                "Sub",
                "Auth",
                root,
                "series",
                [new ChapterInfo("10-a", "Title Only", ChapterKind.Chapter, 10, path)],
                false,
                false,
                Array.Empty<ReferenceSetInfo>());
            var doc = BookPrintAssembler.FromBook(book, seriesTitle: "Series Title", rights: "©");
            await Assert.That(doc.Cover.Series).IsEqualTo("Series Title");
            await Assert.That(doc.Chapters.Count).IsEqualTo(1);

            var bare = BookPrintAssembler.FromChapterMarkdown("Plain text only.\n");
            await Assert.That(bare.Id).IsEqualTo("chapter");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Metadata_apply_callouts_generic_h1_and_aliases()
    {
        var text = "# My Title\n\n> [!loc] Bridge\n> [!chars] A, B\n> [!note] n\n> [!point_of_view] X\n\nBody\n";
        var (meta, body, format) = ManuscriptMetadata.Parse(text);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Callout);
        await Assert.That(meta.Location).IsEqualTo("Bridge");
        await Assert.That(meta.Characters).IsEqualTo("A, B");
        await Assert.That(meta.Pov).IsEqualTo("X");
        await Assert.That(body).Contains("Body");

        var applied = ManuscriptMetadata.ApplyCallouts("# Old\n\nBody\n", new ManuscriptChapterMetadata
        {
            Title = "New Title",
            Date = "d",
            Location = "L",
        });
        await Assert.That(applied).Contains("# New Title");
        await Assert.That(applied).Contains("> [!date] d");

        var chapterForm = ManuscriptMetadata.ApplyCallouts(
            "# Chapter 1 - Old\n\nBody\n",
            new ManuscriptChapterMetadata { Number = "2", Title = "New" });
        await Assert.That(chapterForm).Contains("# Chapter 2 - New");

        var inserted = ManuscriptMetadata.ApplyCallouts(
            "No heading yet\n",
            new ManuscriptChapterMetadata { Number = "3", Title = "Inserted" });
        await Assert.That(inserted).Contains("# Chapter 3 - Inserted");

        var titleOnly = ManuscriptMetadata.ApplyCallouts(
            "No heading yet\n",
            new ManuscriptChapterMetadata { Title = "Solo" });
        await Assert.That(titleOnly).Contains("# Solo");
    }

    [Test]
    public async Task Metadata_yaml_naive_lists_and_tags()
    {
        var md = """
            ---
            # comment
            tags:
              - alpha
              - beta
            characters:
              - Mira
              - James
            location: "Station"
            weird: 42
            ---

            # Go

            Text.
            """;
        var (meta, _, format) = ManuscriptMetadata.Parse(md);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Yaml);
        await Assert.That(meta.Characters).Contains("Mira");
        await Assert.That(meta.Extra.ContainsKey("tags")).IsTrue();
    }

    [Test]
    public async Task Markdown_exporter_bookinfo_and_errors()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-mdex-{Guid.NewGuid():N}");
        var bookDir = Path.Combine(root, "book");
        var chapters = Path.Combine(bookDir, "Chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(bookDir, "book.yaml"), "title: T\nsubtitle: S\nauthor: A\ndebug_mode: true\n");
        File.WriteAllText(Path.Combine(chapters, "1-a.md"), "# A\n\nHi.\n");
        var outDir = Path.Combine(root, "out");
        try
        {
            var paths = ManuscriptMarkdownExporter.ExportBookFolder(bookDir, outDir, "book-id", "series");
            await Assert.That(File.Exists(paths.ReaderMarkdownPath)).IsTrue();
            await Assert.That(File.Exists(paths.AuthorMarkdownPath!)).IsTrue();

            var catalogBook = new BookInfo(
                "book-id",
                "T",
                "S",
                "A",
                bookDir,
                "series",
                [new ChapterInfo("1-a", "A", ChapterKind.Chapter, 1, Path.Combine(chapters, "1-a.md"))],
                false,
                true,
                Array.Empty<ReferenceSetInfo>());
            var paths2 = ManuscriptMarkdownExporter.ExportBook(catalogBook, Path.Combine(outDir, "via"));
            await Assert.That(File.Exists(paths2.HtmlPath!)).IsTrue();

            await Assert.That(() => ManuscriptMarkdownExporter.ExportBookFolder(
                    Path.Combine(root, "missing"), outDir, "x"))
                .ThrowsExactly<DirectoryNotFoundException>();

            await Assert.That(() => ManuscriptMarkdownExporter.ExportBook(
                    new BookPrintDocument("empty", new BookPrintCover("t", null, null, null, null), [], false),
                    outDir))
                .ThrowsExactly<InvalidOperationException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Page_breaks_and_pdf_with_preface()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-pb-{Guid.NewGuid():N}");
        var book = Path.Combine(root, "book");
        var chapters = Path.Combine(book, "chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Book\n");
        File.WriteAllText(Path.Combine(chapters, "000-preface.md"), "# Preface\n\nIntro words.\n");
        File.WriteAllText(Path.Combine(chapters, "001-one.md"), """
            ---
            date: "1"
            pov: James
            ---

            # Chapter 1 - One

            Body one.
            """);
        var outDir = Path.Combine(root, "out");
        try
        {
            var paths = BookPrintExporter.ExportBookFolder(book, outDir, "s", "book", new BookPrintOptions
            {
                IncludeCover = true,
                ShowAllMetadataTags = false,
            });
            await Assert.That(File.Exists(paths.PdfPath)).IsTrue();
            var md = await File.ReadAllTextAsync(paths.MarkdownPath);
            await Assert.That(md).Contains("# Preface");
            await Assert.That(md).Contains("# Chapter 1 - One");
            await Assert.That(md).DoesNotContain("pov:");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Markdown_exporter_lowercase_chapters_and_nonnumeric_stems()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-md2-{Guid.NewGuid():N}");
        var bookDir = Path.Combine(root, "book");
        var chapters = Path.Combine(bookDir, "chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(bookDir, "book.yaml"), "title: T\nsubtitle: Sub\n");
        File.WriteAllText(Path.Combine(chapters, "prologue.md"), "# Prologue\n\nHi.\n");
        File.WriteAllText(Path.Combine(chapters, "2.5-mid.md"), "# Mid\n\nMid.\n");
        var outDir = Path.Combine(root, "out");
        try
        {
            var paths = ManuscriptMarkdownExporter.ExportBookFolder(
                bookDir,
                outDir,
                "book-id",
                options: new ManuscriptMarkdownExportOptions
                {
                    IncludeAuthorMarkdown = false,
                    IncludeHtml = false,
                });
            await Assert.That(File.Exists(paths.ReaderMarkdownPath)).IsTrue();
            await Assert.That(paths.AuthorMarkdownPath).IsNull();
            await Assert.That(paths.HtmlPath).IsNull();

            var doc = new BookPrintDocument(
                "b",
                new BookPrintCover("Title", "Subtitle", null, null, null),
                [BookPrintAssembler.FromChapterMarkdown("# C\n\nB\n", "c")],
                false);
            var withHtml = ManuscriptMarkdownExporter.ExportBook(doc, Path.Combine(outDir, "sub"));
            await Assert.That(File.Exists(withHtml.HtmlPath!)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Editorial_profiles_and_html_metadata_transform()
    {
        await Assert.That(EditorialProfiles.FictionNeutral().LexiconEnabled).IsFalse();
        await Assert.That(EditorialProfiles.Nonfiction().Profile).IsEqualTo(EditorialProfile.Nonfiction);
        var calypso = EditorialProfiles.Calypso();
        await Assert.That(calypso.LexiconEnabled).IsTrue();

        var dir = Path.Combine(Path.GetTempPath(), $"ms-one-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "001.md");
        File.WriteAllText(path, """
            # Ch

            > [!date] 1
            > [!time] 2
            > [!pov] X

            Body.
            """);
        var htmlPath = Path.Combine(Path.GetTempPath(), $"ms-html-{Guid.NewGuid():N}.html");
        try
        {
            var assembled = BookPrintAssembler.AssembleReaderMarkdownFromFiles([path], authorMode: true);
            ManuscriptDocumentEmitters.WriteHtml("T", assembled, htmlPath, stylesheetPath: null, showAllTags: true);
            var html = await File.ReadAllTextAsync(htmlPath);
            await Assert.That(html.Contains("chapter-metadata") || html.Contains("DATE") || html.Contains("[!date]") || html.Contains("sl-")).IsTrue();
            ManuscriptDocumentEmitters.WriteHtml("T", assembled, htmlPath + "2", null, showAllTags: false);
            ManuscriptDocumentEmitters.WritePlainText(assembled, htmlPath + ".txt", showAllTags: true);
            await Assert.That(File.Exists(htmlPath + ".txt")).IsTrue();
            _ = ManuscriptDocumentEmitters.ConcatenateChapterMarkdown([path]);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
            try { File.Delete(htmlPath); } catch { /* ignore */ }
            try { File.Delete(htmlPath + "2"); } catch { /* ignore */ }
            try { File.Delete(htmlPath + ".txt"); } catch { /* ignore */ }
        }
    }
}
