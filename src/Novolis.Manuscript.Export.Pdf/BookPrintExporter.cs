using Novolis.Manuscript;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>
/// Book-folder print export used by CLI orchestration: writes Markdown, HTML, TXT, and PDF.
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

        var document = BookDocument.Open(bookDirectory);
        var settings = options.ResolveSettings(document.DirectoryPath);
        var showAll = options.ResolveShowAllMetadataTags(false);

        var seriesTitle = options.SeriesTitle
                          ?? document.SeriesTitle
                          ?? ResolveSeriesTitle(bookDirectory, seriesId)
                          ?? seriesId;
        var rights = options.Rights ?? document.Rights;

        Directory.CreateDirectory(outputDirectory);
        var stem = Path.Combine(outputDirectory, bookId);
        var mdPath = stem + ".md";
        var htmlPath = stem + ".html";
        var txtPath = stem + ".txt";

        if (options.PdfOutput is BookPdfOutput.Combine or BookPdfOutput.Both)
        {
            WriteCompanions(
                document,
                bookDirectory,
                document.Title,
                markdownAuthorMode: showAll,
                settings,
                seriesTitle,
                mdPath,
                htmlPath,
                txtPath,
                contentRootHint: Directory.GetParent(bookDirectory)?.FullName);
        }

        var pdf = BookPdfWriter.Write(
            document,
            outputDirectory,
            bookId,
            options.PdfOutput,
            settings,
            seriesTitle,
            rights);

        return new BookPrintPaths(
            Path.GetFullPath(mdPath),
            Path.GetFullPath(htmlPath),
            Path.GetFullPath(txtPath),
            pdf.CombinedPdfPath ?? pdf.ChaptersDirectory!);
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

        var document = BookDocument.Open(book.DirectoryPath);
        var settings = options.ResolveSettings(document.DirectoryPath);
        var showAll = options.ResolveShowAllMetadataTags(book.DebugMode);
        var seriesTitle = options.SeriesTitle
                          ?? document.SeriesTitle
                          ?? ResolveSeriesTitle(book.DirectoryPath, book.SeriesId)
                          ?? book.SeriesId;
        var rights = options.Rights ?? document.Rights;

        Directory.CreateDirectory(outputDirectory);
        var stem = Path.Combine(outputDirectory, book.Id);
        var mdPath = stem + ".md";
        var htmlPath = stem + ".html";
        var txtPath = stem + ".txt";

        if (options.PdfOutput is BookPdfOutput.Combine or BookPdfOutput.Both)
        {
            WriteCompanions(
                document,
                book.DirectoryPath,
                document.Title,
                markdownAuthorMode: showAll,
                settings,
                seriesTitle,
                mdPath,
                htmlPath,
                txtPath,
                contentRootHint: null);
        }

        var pdf = BookPdfWriter.Write(
            document,
            outputDirectory,
            book.Id,
            options.PdfOutput,
            settings,
            seriesTitle,
            rights);

        return new BookPrintPaths(
            Path.GetFullPath(mdPath),
            Path.GetFullPath(htmlPath),
            Path.GetFullPath(txtPath),
            pdf.CombinedPdfPath ?? pdf.ChaptersDirectory!);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Series.yaml title resolution filesystem edges.")]
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

    static void WriteCompanions(
        BookDocument document,
        string bookDirectory,
        string title,
        bool markdownAuthorMode,
        ManuscriptPrintSettings settings,
        string? seriesTitle,
        string mdPath,
        string htmlPath,
        string txtPath,
        string? contentRootHint)
    {
        var markdown = BookPrintAssembler.AssembleReaderMarkdownFromFiles(
            document.Chapters.Select(c => c.FilePath),
            authorMode: markdownAuthorMode,
            includePublicDateline: settings.IncludePublicDateline);
        ManuscriptDocumentEmitters.WriteMarkdown(markdown, mdPath);

        var css = StylesheetLocator.Find(bookDirectory, contentRootHint: contentRootHint);
        var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(document.Author))
            meta["author"] = document.Author!;
        if (!string.IsNullOrWhiteSpace(seriesTitle))
            meta["series"] = seriesTitle;

        ManuscriptDocumentEmitters.WriteHtml(title, markdown, htmlPath, css, markdownAuthorMode, meta);
        ManuscriptDocumentEmitters.WritePlainText(markdown, txtPath, markdownAuthorMode);
    }
}
