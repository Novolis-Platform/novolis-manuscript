using Novolis.Manuscript;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Paths written by a multi-format book print export.</summary>
/// <param name="MarkdownPath">Combined chapter Markdown.</param>
/// <param name="HtmlPath">HTML companion.</param>
/// <param name="TextPath">Plain-text companion.</param>
/// <param name="PdfPath">QuestPDF output.</param>
public sealed record BookPrintPaths(
    string MarkdownPath,
    string HtmlPath,
    string TextPath,
    string PdfPath);

/// <summary>
/// Book-folder print export used by CLI orchestration: writes Markdown, HTML, TXT, and PDF
/// with books-grade QuestPDF fidelity.
/// </summary>
public static class BookPrintExporter
{
    /// <summary>
    /// Exports a book directory to <paramref name="outputDirectory"/> as
    /// <c>{bookId}.md</c>, <c>.html</c>, <c>.txt</c>, and <c>.pdf</c>.
    /// </summary>
    /// <param name="bookDirectory">Book root containing <c>book.yaml</c> and <c>Chapters/</c> or <c>chapters/</c>.</param>
    /// <param name="outputDirectory">Destination folder for artifacts.</param>
    /// <param name="seriesId">Series id (cover / meta; may be empty for standalone).</param>
    /// <param name="bookId">Book id used for output file stems.</param>
    /// <param name="options">Optional print options.</param>
    /// <returns>Absolute paths of written artifacts.</returns>
    public static BookPrintPaths ExportBookFolder(
        string bookDirectory,
        string outputDirectory,
        string seriesId,
        string bookId,
        BookPrintOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(bookId);
        options ??= new BookPrintOptions();

        if (!Directory.Exists(bookDirectory))
            throw new DirectoryNotFoundException($"Book directory not found: {bookDirectory}");

        var book = LoadBookFromDirectory(bookDirectory, string.IsNullOrWhiteSpace(seriesId) ? null : seriesId, bookId);
        var settings = options.ResolveSettings();
        var showAll = options.ResolveShowAllMetadataTags(book.DebugMode);

        var seriesTitle = options.SeriesTitle
                          ?? ResolveSeriesTitle(bookDirectory, seriesId)
                          ?? seriesId;
        var rights = options.Rights
                     ?? BookYaml.GetString(BookYaml.LoadFile(Path.Combine(bookDirectory, "book.yaml")), "rights")
                     ?? BookYaml.GetString(BookYaml.LoadFile(Path.Combine(bookDirectory, "book.yaml")), "copyright");

        Directory.CreateDirectory(outputDirectory);
        var stem = Path.Combine(outputDirectory, bookId);
        var mdPath = stem + ".md";
        var htmlPath = stem + ".html";
        var txtPath = stem + ".txt";
        var pdfPath = stem + ".pdf";

        var markdown = ManuscriptDocumentEmitters.ConcatenateChapterMarkdown(book.Chapters.Select(c => c.FilePath));
        ManuscriptDocumentEmitters.WriteMarkdown(markdown, mdPath);

        var css = StylesheetLocator.Find(bookDirectory, contentRootHint: Directory.GetParent(bookDirectory)?.FullName);
        var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(book.Author))
            meta["author"] = book.Author!;
        if (!string.IsNullOrWhiteSpace(seriesTitle))
            meta["series"] = seriesTitle;

        ManuscriptDocumentEmitters.WriteHtml(book.Title, markdown, htmlPath, css, showAll, meta);
        ManuscriptDocumentEmitters.WritePlainText(markdown, txtPath, showAll);

        QuestPdfDocuments.WriteBookPdf(
            markdown,
            pdfPath,
            new QuestPdfDocuments.BookCoverMeta(book.Title, book.Subtitle, seriesTitle, book.Author, rights),
            showAll,
            settings);

        return new BookPrintPaths(
            Path.GetFullPath(mdPath),
            Path.GetFullPath(htmlPath),
            Path.GetFullPath(txtPath),
            Path.GetFullPath(pdfPath));
    }

    /// <summary>
    /// Exports an already-loaded <see cref="BookInfo"/> to multi-format artifacts under <paramref name="outputDirectory"/>.
    /// </summary>
    public static BookPrintPaths ExportBook(
        BookInfo book,
        string outputDirectory,
        BookPrintOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        options ??= new BookPrintOptions();

        var settings = options.ResolveSettings();
        var showAll = options.ResolveShowAllMetadataTags(book.DebugMode);
        var seriesTitle = options.SeriesTitle
                          ?? ResolveSeriesTitle(book.DirectoryPath, book.SeriesId)
                          ?? book.SeriesId;
        var yaml = BookYaml.LoadFile(Path.Combine(book.DirectoryPath, "book.yaml"));
        var rights = options.Rights
                     ?? BookYaml.GetString(yaml, "rights")
                     ?? BookYaml.GetString(yaml, "copyright");

        Directory.CreateDirectory(outputDirectory);
        var stem = Path.Combine(outputDirectory, book.Id);
        var mdPath = stem + ".md";
        var htmlPath = stem + ".html";
        var txtPath = stem + ".txt";
        var pdfPath = stem + ".pdf";

        var markdown = ManuscriptDocumentEmitters.ConcatenateChapterMarkdown(book.Chapters.Select(c => c.FilePath));
        ManuscriptDocumentEmitters.WriteMarkdown(markdown, mdPath);

        var css = StylesheetLocator.Find(book.DirectoryPath);
        var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(book.Author))
            meta["author"] = book.Author!;
        if (!string.IsNullOrWhiteSpace(seriesTitle))
            meta["series"] = seriesTitle!;

        ManuscriptDocumentEmitters.WriteHtml(book.Title, markdown, htmlPath, css, showAll, meta);
        ManuscriptDocumentEmitters.WritePlainText(markdown, txtPath, showAll);

        QuestPdfDocuments.WriteBookPdf(
            markdown,
            pdfPath,
            new QuestPdfDocuments.BookCoverMeta(book.Title, book.Subtitle, seriesTitle, book.Author, rights),
            showAll,
            settings);

        return new BookPrintPaths(
            Path.GetFullPath(mdPath),
            Path.GetFullPath(htmlPath),
            Path.GetFullPath(txtPath),
            Path.GetFullPath(pdfPath));
    }

    static string? ResolveSeriesTitle(string bookDirectory, string? seriesId)
    {
        var parent = Directory.GetParent(bookDirectory)?.FullName;
        if (parent != null)
        {
            var seriesYaml = Path.Combine(parent, "series.yaml");
            if (File.Exists(seriesYaml))
            {
                var yaml = BookYaml.LoadFile(seriesYaml);
                return BookYaml.GetString(yaml, "title")
                       ?? BookYaml.GetString(yaml, "name")
                       ?? seriesId;
            }
        }

        return seriesId;
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
                var sortKey = TryParsePrefix(stem);
                chapters.Add(new ChapterInfo(stem, ReadHeadingTitle(file) ?? stem, ChapterKind.Chapter, sortKey, file));
            }
        }

        var apDir = ResolveDir(bookDirectory, protocol ? "Appendices" : "appendices", "appendices", "Appendices");
        if (apDir is not null)
        {
            foreach (var file in Directory.GetFiles(apDir, "*.md")
                         .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal))
            {
                var stem = Path.GetFileNameWithoutExtension(file);
                chapters.Add(new ChapterInfo(stem, ReadHeadingTitle(file) ?? stem, ChapterKind.Appendix, chapters.Count, file));
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

    static string? ReadHeadingTitle(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith("---", StringComparison.Ordinal))
                continue;
            if (t.StartsWith('#'))
                return t.TrimStart('#').Trim();
            break;
        }

        return null;
    }
}
