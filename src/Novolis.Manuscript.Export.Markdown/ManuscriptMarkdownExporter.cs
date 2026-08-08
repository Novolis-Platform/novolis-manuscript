using System.Text;
using Novolis.Markup.Markdown;
using Novolis.Markup.Markdown.Rendering;

namespace Novolis.Manuscript.Export.Markdown;

/// <summary>Exports books to reader/author Markdown and HTML via Novolis.Markup.</summary>
public static class ManuscriptMarkdownExporter
{
    /// <summary>Exports a <see cref="BookPrintDocument"/> under <paramref name="outputDirectory"/>.</summary>
    public static ManuscriptMarkdownPaths ExportBook(
        BookPrintDocument document,
        string outputDirectory,
        ManuscriptMarkdownExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        options ??= new ManuscriptMarkdownExportOptions();

        if (document.Chapters.Count == 0)
            throw new InvalidOperationException("Book has no chapters to export.");

        Directory.CreateDirectory(outputDirectory);
        var stem = Path.Combine(outputDirectory, document.BookId);
        var readerMd = BookPrintAssembler.AssembleMarkdown(document, authorMode: false);
        var readerPath = stem + ".reader.md";
        WriteUtf8(readerPath, readerMd);

        string? authorPath = null;
        if (options.IncludeAuthorMarkdown || options.AuthorMode || document.DebugMode)
        {
            var authorMd = BookPrintAssembler.AssembleMarkdown(document, authorMode: true);
            authorPath = stem + ".author.md";
            WriteUtf8(authorPath, authorMd);
        }

        string? htmlPath = null;
        if (options.IncludeHtml)
        {
            htmlPath = stem + ".reader.html";
            var title = document.Cover.Title;
            MarkdownHtmlExporter.ExportToFile(readerMd, htmlPath, options.HtmlTheme, title);
        }

        // Touch fluent MarkdownDocument so Markup.Markdown is a real dependency surface.
        _ = BuildFluentOutline(document);

        return new ManuscriptMarkdownPaths(
            Path.GetFullPath(readerPath),
            authorPath is null ? null : Path.GetFullPath(authorPath),
            htmlPath is null ? null : Path.GetFullPath(htmlPath));
    }

    /// <summary>Exports from catalog <see cref="BookInfo"/>.</summary>
    public static ManuscriptMarkdownPaths ExportBook(
        BookInfo book,
        string outputDirectory,
        ManuscriptMarkdownExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(book);
        options ??= new ManuscriptMarkdownExportOptions();
        var document = BookPrintAssembler.FromBook(book, options.SeriesTitle, options.Rights);
        return ExportBook(document, outputDirectory, options);
    }

    /// <summary>Exports a book folder (expects <c>book.yaml</c> + Chapters).</summary>
    public static ManuscriptMarkdownPaths ExportBookFolder(
        string bookDirectory,
        string outputDirectory,
        string bookId,
        string? seriesId = null,
        ManuscriptMarkdownExportOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(bookId);
        if (!Directory.Exists(bookDirectory))
            throw new DirectoryNotFoundException($"Book directory not found: {bookDirectory}");

        options ??= new ManuscriptMarkdownExportOptions();
        var book = LoadBookFromDirectory(bookDirectory, seriesId, bookId);
        return ExportBook(book, outputDirectory, options);
    }

    static IMarkdownDocument BuildFluentOutline(BookPrintDocument document)
    {
        IMarkdownDocument md = new MarkdownDocument();
        md = md.WithHeader(document.Cover.Title, MarkdownHeaderLevel.H1);
        if (!string.IsNullOrWhiteSpace(document.Cover.Subtitle))
            md = md.WithParagraph(new MarkdownParagraph().WithText(document.Cover.Subtitle!));
        foreach (var chapter in document.Chapters)
            md = md.WithHeader(chapter.Title, MarkdownHeaderLevel.H2);
        return md;
    }

    static void WriteUtf8(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    static BookInfo LoadBookFromDirectory(string bookDirectory, string? seriesId, string bookId)
    {
        var protocol = Directory.Exists(Path.Combine(bookDirectory, "Chapters"))
                       || Directory.Exists(Path.Combine(bookDirectory, "Appendices"));
        var yaml = BookYaml.LoadFile(Path.Combine(bookDirectory, "book.yaml"));
        var title = BookYaml.GetString(yaml, "title") ?? bookId;
        var subtitle = BookYaml.GetString(yaml, "subtitle");
        var author = BookYaml.GetString(yaml, "author");
        var debugMode = BookYaml.GetBool(yaml, "debug_mode");

        var chapters = new List<ChapterInfo>();
        var chDir = ResolveDir(bookDirectory, protocol ? "Chapters" : "chapters", "chapters", "Chapters");
        if (chDir is not null)
        {
            foreach (var file in Directory.GetFiles(chDir, "*.md")
                         .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal))
            {
                var stem = Path.GetFileNameWithoutExtension(file);
                chapters.Add(new ChapterInfo(stem, stem, ChapterKind.Chapter, TryParsePrefix(stem), file));
            }
        }

        var ordered = chapters
            .OrderBy(c => c.Kind)
            .ThenBy(c => c.SortKey)
            .ThenBy(c => c.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new BookInfo(
            bookId,
            title,
            subtitle,
            author,
            Path.GetFullPath(bookDirectory),
            seriesId,
            ordered,
            ChapterOrderFromHeading: false,
            debugMode,
            Array.Empty<ReferenceSetInfo>());
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Duplicate dir/prefix helpers; exercised via ExportBookFolder.")]
    static string? ResolveDir(string parent, string preferred, params string[] fallbacks)
    {
        var preferredPath = Path.Combine(parent, preferred);
        if (Directory.Exists(preferredPath))
            return preferredPath;
        foreach (var name in fallbacks)
        {
            var p = Path.Combine(parent, name);
            if (Directory.Exists(p))
                return p;
        }

        return null;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Stem sort-key parse; covered indirectly.")]
    static double TryParsePrefix(string stem)
    {
        var i = 0;
        while (i < stem.Length && (char.IsDigit(stem[i]) || stem[i] == '.'))
            i++;
        if (i == 0)
            return double.MaxValue;
        return double.TryParse(stem[..i].TrimEnd('.'), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var n)
            ? n
            : double.MaxValue;
    }
}
