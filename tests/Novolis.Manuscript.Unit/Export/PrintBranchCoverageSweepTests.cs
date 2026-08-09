using Novolis.Manuscript;
using Novolis.Manuscript.Export.Markdown;
using Novolis.Manuscript.Export.Pdf;

namespace Novolis.Manuscript.Unit.Export;

public sealed class PrintBranchCoverageSweepTests
{
    [Test]
    public async Task Book_print_protocol_chapters_series_appendices_and_export_book()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-br-{Guid.NewGuid():N}");
        var series = Path.Combine(root, "series");
        var book = Path.Combine(series, "book");
        var chapters = Path.Combine(book, "Chapters");
        var appendices = Path.Combine(book, "Appendices");
        Directory.CreateDirectory(chapters);
        Directory.CreateDirectory(appendices);
        File.WriteAllText(Path.Combine(series, "series.yaml"), "title: Series Name\n");
        File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Book T\nsubtitle: Sub\nauthor: Auth\nrights: ©R\n");
        File.WriteAllText(Path.Combine(chapters, "001-one.md"), "\uFEFF# Chapter 1 - One\n\n> [!date] D\n\nProse.\n");
        File.WriteAllText(Path.Combine(chapters, "alpha.md"), "# Alpha\n\nNo number prefix.\n");
        File.WriteAllText(Path.Combine(appendices, "A-notes.md"), "# Notes\n\nAppendix body.\n");
        File.WriteAllText(Path.Combine(chapters, "blank-title.md"), "\n\nJust body, no heading.\n");
        var outDir = Path.Combine(root, "out");
        try
        {
            var paths = BookPrintExporter.ExportBookFolder(book, outDir, "series", "book-id", new BookPrintOptions
            {
                IncludeCover = true,
                SeriesTitle = null,
                Rights = null,
                ShowAllMetadataTags = true,
            });
            await Assert.That(File.Exists(paths.PdfPath)).IsTrue();
            var md = await File.ReadAllTextAsync(paths.MarkdownPath);
            await Assert.That(md).Contains("One");
            await Assert.That(md).Contains("Notes");

            var catalog = new BookInfo(
                "via",
                "T",
                "S",
                "A",
                book,
                "series",
                [
                    new ChapterInfo("001-one", "One", ChapterKind.Chapter, 1,
                        Path.Combine(chapters, "001-one.md")),
                ],
                false,
                false,
                Array.Empty<ReferenceSetInfo>());
            var paths2 = BookPrintExporter.ExportBook(catalog, Path.Combine(outDir, "via"), new BookPrintOptions
            {
                SeriesTitle = "Override Series",
                Rights = "© override",
            });
            await Assert.That(File.Exists(paths2.PdfPath)).IsTrue();

            await Assert.That(() => BookPrintExporter.ExportBookFolder(
                    Path.Combine(root, "missing"), outDir, "s", "b"))
                .ThrowsExactly<DirectoryNotFoundException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Book_print_fallback_chapters_dir_and_series_name_key()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-br2-{Guid.NewGuid():N}");
        var series = Path.Combine(root, "ser");
        var book = Path.Combine(series, "bk");
        // preferred "chapters" missing; fallback "Chapters" via ResolveDir when protocol=false
        var chapters = Path.Combine(book, "Chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(series, "series.yaml"), "name: Named Series\n");
        File.WriteAllText(Path.Combine(book, "book.yaml"), "{}\n");
        File.WriteAllText(Path.Combine(chapters, "10.md"), "---\ndate: x\n---\n\n# Ten\n\nBody\n");
        try
        {
            var paths = BookPrintExporter.ExportBookFolder(book, Path.Combine(root, "out"), "", "bk");
            await Assert.That(File.Exists(paths.TextPath)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Markdown_exporter_protocol_appendices_flag_and_author_mode_option()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-mdbr-{Guid.NewGuid():N}");
        var book = Path.Combine(root, "book");
        Directory.CreateDirectory(Path.Combine(book, "Appendices"));
        Directory.CreateDirectory(Path.Combine(book, "chapters"));
        File.WriteAllText(Path.Combine(book, "book.yaml"), "author: A\n");
        File.WriteAllText(Path.Combine(book, "chapters", "1.md"), "# C\n\nB\n");
        try
        {
            var paths = ManuscriptMarkdownExporter.ExportBookFolder(
                book,
                Path.Combine(root, "out"),
                "id",
                options: new ManuscriptMarkdownExportOptions
                {
                    AuthorMode = true,
                    IncludeAuthorMarkdown = false,
                    IncludeHtml = true,
                });
            await Assert.That(File.Exists(paths.AuthorMarkdownPath!)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Assembler_synthetic_heading_from_yaml_title_and_public_extra()
    {
        var withNumber = BookPrintAssembler.FromChapterMarkdown("""
            ---
            title: Yaml Title
            number: "7"
            date: "2495.001"
            location: Dock
            mystery: hush
            ---

            Body only, no H1 in remainder after strip.
            """);
        await Assert.That(withNumber.HeadingMarkdown).Contains("Chapter 7");
        await Assert.That(withNumber.Title).IsEqualTo("Yaml Title");
        await Assert.That(withNumber.HiddenFields["mystery"]).IsEqualTo("hush");

        var titleOnly = BookPrintAssembler.FromChapterMarkdown("""
            ---
            title: Solo Title
            ---

            Prose.
            """);
        await Assert.That(titleOnly.HeadingMarkdown).IsEqualTo("# Solo Title");

        var author = BookPrintAssembler.AssembleMarkdown(
            new BookPrintDocument(
                "b",
                new BookPrintCover("t", null, null, null, null),
                [withNumber],
                DebugMode: true),
            authorMode: false);
        await Assert.That(author).Contains("> ");

        var noHeadingTitle = BookPrintAssembler.FromChapterMarkdown(
            "---\ndate: d\n---\n\nProse\n",
            id: " ");
        await Assert.That(noHeadingTitle.Id).IsEqualTo("chapter");

        var h2 = BookPrintAssembler.FromChapterMarkdown("## Not H1\n\nBody\n");
        await Assert.That(h2.BodyMarkdown).Contains("## Not H1");

        var hashNoSpace = BookPrintAssembler.FromChapterMarkdown("#NoSpace\n\nBody\n");
        await Assert.That(hashNoSpace.BodyMarkdown).Contains("#NoSpace");

        // Public tag in Extra should not become hidden
        var mergeBlank = BookPrintAssembler.MergeDateTimeLines(
        [
            ("date", "   "),
            ("system", "Sol"),
        ]);
        await Assert.That(mergeBlank).Contains("Sol");

        var synthetic = new ChapterPrintView(
            "x",
            "Synth",
            "",
            [("date", "1")],
            ["1"],
            new Dictionary<string, string>(),
            "Body line",
            null,
            ManuscriptMetadataFormat.None);
        var assembled = BookPrintAssembler.AssembleMarkdown(
            new BookPrintDocument("b", new BookPrintCover("t", null, null, null, null), [synthetic], false));
        await Assert.That(assembled).Contains("# Synth");
        await Assert.That(assembled).Contains("Body line");
    }

    [Test]
    public async Task Markdown_exporter_chapters_capital_fallback_and_empty_chapters()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-mdfb-{Guid.NewGuid():N}");
        var book = Path.Combine(root, "book");
        var chapters = Path.Combine(book, "Chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(book, "book.yaml"), "title: T\n");
        File.WriteAllText(Path.Combine(chapters, "badprefix.md"), "# Bad\n\nB\n");
        File.WriteAllText(Path.Combine(chapters, "1.2.3-x.md"), "# Num\n\nB\n");
        try
        {
            var paths = ManuscriptMarkdownExporter.ExportBookFolder(book, Path.Combine(root, "out"), "id");
            await Assert.That(File.Exists(paths.ReaderMarkdownPath)).IsTrue();

            // No chapters directory at all
            var emptyBook = Path.Combine(root, "empty");
            Directory.CreateDirectory(emptyBook);
            File.WriteAllText(Path.Combine(emptyBook, "book.yaml"), "title: E\n");
            await Assert.That(() => ManuscriptMarkdownExporter.ExportBookFolder(
                    emptyBook, Path.Combine(root, "out2"), "e"))
                .ThrowsExactly<InvalidOperationException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Rich_markdown_pdf_and_plain_text_edges()
    {
        var md = """
            # Title

            Intro paragraph with **strong** and *em* and `code` and [link](https://example.com).

            ***

            > Short dateline quote

            > "Dialogue lead quote that should not be dateline style."

            > A longer blockquote that is ordinary callout prose for the renderer path.

            1. Ordered one
            2. Ordered two

            - Bullet alpha
            - Bullet beta

            | H1 | H2 |
            | --- | --- |
            | a | b |
            | c | d |

            ```
            fenced
            ```

                indented code

            <div>html ignored</div>

            # Chapter Two

            Second chapter body.
            """;
        var root = Path.Combine(Path.GetTempPath(), $"ms-rich-{Guid.NewGuid():N}");
        var book = Path.Combine(root, "book");
        var chapters = Path.Combine(book, "chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Rich\n");
        File.WriteAllText(Path.Combine(chapters, "1.md"), md);
        try
        {
            var paths = BookPrintExporter.ExportBookFolder(book, Path.Combine(root, "out"), "s", "rich");
            var txt = await File.ReadAllTextAsync(paths.TextPath);
            await Assert.That(txt).Contains("Title");
            await Assert.That(txt).Contains("Ordered");
            await Assert.That(File.Exists(paths.PdfPath)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Metadata_naive_yaml_flush_and_inline_split_edges()
    {
        var brokenYaml = """
            ---
            date: 
              - only
            characters:
            locations:
              - Dock
            # comment mid
            tags:
            status: draft
            note:
            ---

            # Chapter 2 - X

            > [!date] override-after? 

            Body
            """;
        // YAML wins at start; callouts after H1 still apply in hybrid path
        var (meta, body, format) = ManuscriptMetadata.Parse(brokenYaml);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Yaml);
        await Assert.That(body).Contains("Body");
        _ = meta;

        var multi = """
            # T

            > [!date] 1 [!time] 2 [!loc] L [!chars] A [!note] n [!status] s [!tags] t [!custom] c [!point_of_view] P

            Body
            """;
        var (m2, _, _) = ManuscriptMetadata.Parse(multi);
        await Assert.That(m2.Date).IsEqualTo("1");
        await Assert.That(m2.Time).IsEqualTo("2");
        await Assert.That(m2.Location).IsEqualTo("L");
        await Assert.That(m2.Pov).IsEqualTo("P");

        var onlyCalloutsNoH1 = "> [!date] 1\n\nBody without heading.\n";
        var (m3, b3, f3) = ManuscriptMetadata.Parse(onlyCalloutsNoH1);
        await Assert.That(f3).IsEqualTo(ManuscriptMetadataFormat.Callout);
        await Assert.That(m3.Date).IsEqualTo("1");
        await Assert.That(b3).Contains("Body");

        var emptyBodyCallouts = """
            # Only

            > [!date] 1
            """;
        var (m4, b4, _) = ManuscriptMetadata.Parse(emptyBodyCallouts);
        await Assert.That(m4.Date).IsEqualTo("1");
        await Assert.That(b4).Contains("# Only");

        // Number without callouts (chapter heading only)
        var numbered = ManuscriptMetadata.Parse("# Chapter 9 - Solo\n\nProse.\n");
        await Assert.That(numbered.Format).IsEqualTo(ManuscriptMetadataFormat.Callout);
        await Assert.That(numbered.Meta.Number).IsEqualTo("9");

        var wc = ManuscriptMetadata.GetBodyForWordCount("""
            # H

            > [!date] 1

            Word one two
            """);
        await Assert.That(wc).Contains("Word");
        await Assert.That(ManuscriptMetadata.CountWords("# H\n\nWord one two\n")).IsEqualTo(3);

        // Force naive path via invalid YAML that throws
        var naive = """
            ---
            date: [unterminated
            characters:
              - A
              - B
            ---

            # N

            Z
            """;
        var (mn, _, fn) = ManuscriptMetadata.Parse(naive);
        await Assert.That(fn).IsEqualTo(ManuscriptMetadataFormat.Yaml);
        _ = mn;

        // GetBodyForWordCount callout remainder without H1 in remainder
        var calloutOnly = "> [!pov] X\n\nWords here now.\n";
        var bodyWc = ManuscriptMetadata.GetBodyForWordCount(calloutOnly);
        await Assert.That(bodyWc).Contains("Words");
    }
}