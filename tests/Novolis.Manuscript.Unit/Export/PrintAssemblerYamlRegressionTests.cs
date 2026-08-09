using Novolis.Manuscript;
using Novolis.Manuscript.Export.Audio;
using Novolis.Manuscript.Export.Markdown;
using Novolis.Manuscript.Export.Pdf;

namespace Novolis.Manuscript.Unit.Export;

public sealed class PrintAssemblerYamlRegressionTests
{
    const string YamlChapter = """
        ---
        date: 2495.220
        time: "12:30"
        system: K21408
        location: "Duckville Station"
        characters: "James, Mira, Kerlic Vanori"
        pov: James
        status: draft
        ---

        # Chapter 1 - Lunch Break

        James's lunch was spaghetti.
        """;

    const string CalloutChapter = """
        # Chapter 1 - Lunch Break

        > [!date] 2495.220
        > [!time] 12:30
        > [!system] K21408
        > [!location] Duckville Station
        > [!characters] James, Mira, Kerlic Vanori
        > [!pov] James

        James's lunch was spaghetti.
        """;

    [Test]
    public async Task Yaml_reader_markdown_strips_hidden_and_fences()
    {
        var view = BookPrintAssembler.FromChapterMarkdown(YamlChapter, id: "1-lunch");
        await Assert.That(view.Title).IsEqualTo("Lunch Break");
        await Assert.That(view.HeadingMarkdown).Contains("Chapter 1 - Lunch Break");
        await Assert.That(view.ReaderDatelineLines.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(view.ReaderDatelineLines[0]).IsEqualTo("2495.220 12:30");
        await Assert.That(view.HiddenFields.ContainsKey("pov")).IsTrue();
        await Assert.That(view.HiddenFields.ContainsKey("characters")).IsTrue();

        var reader = BookPrintAssembler.AssembleMarkdown(
            new BookPrintDocument("book", new BookPrintCover("T", null, null, null, null), [view], false),
            authorMode: false);

        await Assert.That(reader).DoesNotContain("pov:");
        await Assert.That(reader).DoesNotContain("characters:");
        await Assert.That(reader).DoesNotContain("status:");
        await Assert.That(reader).DoesNotContain("---");
        await Assert.That(reader).DoesNotContain("> [!pov]");
        await Assert.That(reader).DoesNotContain("> [!date]");
        await Assert.That(reader).Contains("> 2495.220 12:30");
        await Assert.That(reader).Contains("> K21408");
        await Assert.That(reader).Contains("> Duckville Station");
        await Assert.That(reader).Contains("James's lunch was spaghetti.");
        await Assert.That(reader.IndexOf("Chapter 1 - Lunch Break", StringComparison.Ordinal))
            .IsLessThan(reader.IndexOf("2495.220", StringComparison.Ordinal));
    }

    [Test]
    public async Task Callout_reader_parity_with_yaml()
    {
        var yaml = BookPrintAssembler.FromChapterMarkdown(YamlChapter);
        var callout = BookPrintAssembler.FromChapterMarkdown(CalloutChapter);
        await Assert.That(callout.ReaderDatelineLines[0]).IsEqualTo(yaml.ReaderDatelineLines[0]);
        await Assert.That(callout.HiddenFields["pov"]).IsEqualTo("James");
    }

    [Test]
    public async Task Author_mode_does_not_dump_private_into_quotes()
    {
        var view = BookPrintAssembler.FromChapterMarkdown(YamlChapter);
        var author = BookPrintAssembler.AssembleMarkdown(
            new BookPrintDocument("book", new BookPrintCover("T", null, null, null, null), [view], true),
            authorMode: true);
        await Assert.That(author).DoesNotContain("> [!pov]");
        await Assert.That(author).DoesNotContain("> [!characters]");
        await Assert.That(author).Contains("> 2495.220 12:30");
        await Assert.That(author).Contains("> Duckville Station");
    }

    [Test]
    public async Task NonFiction_assemble_skips_public_dateline()
    {
        var view = BookPrintAssembler.FromChapterMarkdown(YamlChapter);
        var md = BookPrintAssembler.AssembleMarkdown(
            new BookPrintDocument("book", new BookPrintCover("T", null, null, null, null), [view], false),
            includePublicDateline: false);

        await Assert.That(md).DoesNotContain("> 2495.220");
        await Assert.That(md).DoesNotContain("> K21408");
        await Assert.That(md).Contains("# Chapter 1 - Lunch Break");
        await Assert.That(md).Contains("James's lunch was spaghetti.");
    }

    [Test]
    public async Task ForTextbook_detects_NonFiction_path_and_letter_defaults()
    {
        var nf = Path.Combine("D:", "repos", "books", "src", "NonFiction", "software-engineering", "intro");
        await Assert.That(ManuscriptPrintSettings.IsNonFictionBookPath(nf)).IsTrue();
        await Assert.That(ManuscriptPrintSettings.IsNonFictionBookPath(
            Path.Combine("D:", "repos", "books", "src", "Fiction", "calypso"))).IsFalse();

        var settings = ManuscriptPrintSettings.ForTextbook();
        await Assert.That(settings.PageWidthInches).IsEqualTo(8.5f);
        await Assert.That(settings.PageHeightInches).IsEqualTo(11f);
        await Assert.That(settings.IncludePublicDateline).IsFalse();
        await Assert.That(settings.UseTextbookChrome).IsTrue();
        await Assert.That(settings.CodeFontFamily).IsEqualTo("Consolas");
    }

    [Test]
    public async Task Book_print_txt_html_have_no_yaml_keys()
    {
        var root = CreateBook(YamlChapter);
        var outDir = Path.Combine(Path.GetTempPath(), $"ms-yaml-{Guid.NewGuid():N}");
        try
        {
            var bookDir = Path.Combine(root, "content", "series", "demo", "books", "book-one");
            var paths = BookPrintExporter.ExportBookFolder(bookDir, outDir, "demo", "book-one");
            var txt = await File.ReadAllTextAsync(paths.TextPath);
            var html = await File.ReadAllTextAsync(paths.HtmlPath);
            var md = await File.ReadAllTextAsync(paths.MarkdownPath);

            foreach (var artifact in new[] { txt, html, md })
            {
                await Assert.That(artifact).DoesNotContain("pov:");
                await Assert.That(artifact).DoesNotContain("characters:");
                await Assert.That(artifact).DoesNotContain("date:");
                await Assert.That(artifact).Contains("2495.220");
                await Assert.That(artifact).Contains("Lunch Break");
            }

            await Assert.That(File.Exists(paths.PdfPath)).IsTrue();
        }
        finally
        {
            TryDelete(root);
            TryDelete(outDir);
        }
    }

    [Test]
    public async Task Export_markdown_reader_strips_hidden()
    {
        var root = CreateBook(YamlChapter);
        var outDir = Path.Combine(Path.GetTempPath(), $"ms-md-{Guid.NewGuid():N}");
        try
        {
            var bookDir = Path.Combine(root, "content", "series", "demo", "books", "book-one");
            var paths = ManuscriptMarkdownExporter.ExportBookFolder(bookDir, outDir, "book-one", "demo");
            var reader = await File.ReadAllTextAsync(paths.ReaderMarkdownPath);
            await Assert.That(reader).DoesNotContain("pov:");
            await Assert.That(reader).DoesNotContain("> [!pov]");
            await Assert.That(File.Exists(paths.AuthorMarkdownPath!)).IsTrue();
            var author = await File.ReadAllTextAsync(paths.AuthorMarkdownPath!);
            await Assert.That(author).DoesNotContain("> [!pov]");
            await Assert.That(author).Contains("> 2495.220");
            await Assert.That(File.Exists(paths.HtmlPath!)).IsTrue();
        }
        finally
        {
            TryDelete(root);
            TryDelete(outDir);
        }
    }

    [Test]
    public async Task Audio_normalize_strips_yaml_metadata()
    {
        var spoken = SpeechPlanner.Normalize(YamlChapter, keepTitle: true);
        await Assert.That(spoken).Contains("Lunch Break");
        await Assert.That(spoken).Contains("spaghetti");
        await Assert.That(spoken).DoesNotContain("pov:");
        await Assert.That(spoken).DoesNotContain("characters:");
        await Assert.That(spoken).DoesNotContain("2495.220");
    }

    [Test]
    public async Task Page_break_helper_requires_prior_material()
    {
        await Assert.That(ChapterPageBreaks.ShouldBreakBeforeHeading(false, 1)).IsFalse();
        await Assert.That(ChapterPageBreaks.ShouldBreakBeforeHeading(true, 1)).IsTrue();
        await Assert.That(ChapterPageBreaks.ShouldBreakBeforeHeading(true, 2)).IsFalse();
    }

    [Test]
    public async Task Preface_then_chapter_emits_page_break_decision()
    {
        var preface = "# Preface\n\nYou are not alone.\n";
        var chapter = YamlChapter;
        var combined = BookPrintAssembler.AssembleReaderMarkdownFromFiles(WriteTempChapters(preface, chapter));
        // After assembly, both H1s exist; PDF layer breaks before second H1 when material exists.
        var h1Count = combined.Split('\n').Count(l => l.StartsWith("# ", StringComparison.Ordinal));
        await Assert.That(h1Count).IsEqualTo(2);
        await Assert.That(ChapterPageBreaks.ShouldBreakBeforeHeading(true, 1)).IsTrue();
    }

    [Test]
    public async Task Visibility_public_tags()
    {
        await Assert.That(ChapterMetadataVisibility.IsPublicTag("date")).IsTrue();
        await Assert.That(ChapterMetadataVisibility.IsPublicTag("pov")).IsFalse();
        await Assert.That(ChapterMetadataVisibility.IsHiddenTag("characters")).IsTrue();
    }

    [Test]
    public async Task Yaml_locations_list_coerced()
    {
        var md = """
            ---
            date: "2496.001"
            locations:
              - Hub
              - Calypso
            pov: Marsh
            ---

            # Arrival

            Body.
            """;
        var (meta, _, format) = ManuscriptMetadata.Parse(md);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Yaml);
        await Assert.That(meta.Location).Contains("Hub");
        await Assert.That(meta.Location).Contains("Calypso");
        await Assert.That(meta.Pov).IsEqualTo("Marsh");
    }

    static IEnumerable<string> WriteTempChapters(params string[] bodies)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ms-ch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var paths = new List<string>();
        for (var i = 0; i < bodies.Length; i++)
        {
            var path = Path.Combine(dir, $"{i + 1:000}.md");
            File.WriteAllText(path, bodies[i]);
            paths.Add(path);
        }

        return paths;
    }

    static string CreateBook(string chapterMarkdown)
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-book-{Guid.NewGuid():N}");
        var series = Path.Combine(root, "content", "series", "demo");
        var book = Path.Combine(series, "books", "book-one");
        var chapters = Path.Combine(book, "chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(series, "series.yaml"), "title: Demo\n");
        File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Book One\nauthor: Test\n");
        File.WriteAllText(Path.Combine(chapters, "001-lunch.md"), chapterMarkdown);
        return root;
    }

    static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // ignore
        }
    }
}
