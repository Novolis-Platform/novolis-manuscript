using Novolis.Documents.Skia;
using Novolis.Markup.Markdown;
using Novolis.Markup.Markdown.Documents;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>
/// Writes book PDFs from a <see cref="BookDocument"/>. Combined file, chapter folder, or both.
/// </summary>
public static class BookPdfWriter
{
    /// <summary>
    /// Writes PDFs for <paramref name="book"/> under <paramref name="outputDirectory"/>.
    /// Default <paramref name="output"/> is <see cref="BookPdfOutput.Combine"/>.
    /// </summary>
    public static BookPdfPaths Write(
        BookDocument book,
        string outputDirectory,
        string stem,
        BookPdfOutput output = BookPdfOutput.Combine,
        ManuscriptPrintSettings? settings = null,
        string? seriesTitle = null,
        string? rights = null)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(stem);
        if (book.Chapters.Count == 0)
            throw new InvalidOperationException($"Book '{book.Id}' has no chapters to print.");

        settings ??= ManuscriptPrintSettings.ResolveForDirectory(book.DirectoryPath);
        Directory.CreateDirectory(outputDirectory);

        var cover = new ManuscriptPagedDocumentBuilder.BookCoverMeta(
            book.Title,
            book.Subtitle,
            seriesTitle ?? book.SeriesTitle,
            book.Author,
            rights ?? book.Rights);

        string? combinedPath = null;
        string? chaptersDir = null;
        List<string> chapterPaths = [];

        if (output is BookPdfOutput.Combine or BookPdfOutput.Both)
        {
            combinedPath = Path.GetFullPath(Path.Combine(outputDirectory, stem + ".pdf"));
            var combined = CombineBodies(book.Chapters);
            ManuscriptPagedDocumentBuilder.WriteDocument(
                combined,
                combinedPath,
                cover,
                settings,
                includeToc: settings.IncludeToc,
                includeCover: settings.IncludeCover);
        }

        if (output is BookPdfOutput.Chapters or BookPdfOutput.Both)
        {
            chaptersDir = Path.GetFullPath(Path.Combine(outputDirectory, stem + "-chapters"));
            Directory.CreateDirectory(chaptersDir);
            foreach (var chapter in book.Chapters)
            {
                var fileStem = Path.GetFileNameWithoutExtension(chapter.FilePath);
                var path = Path.GetFullPath(Path.Combine(chaptersDir, fileStem + ".pdf"));
                var chapterCover = new ManuscriptPagedDocumentBuilder.BookCoverMeta(
                    chapter.Title, null, cover.Series, cover.Author, cover.Rights);
                ManuscriptPagedDocumentBuilder.WriteDocument(
                    chapter.Body,
                    path,
                    chapterCover,
                    settings,
                    includeToc: false,
                    includeCover: false);
                chapterPaths.Add(path);
            }
        }

        return new BookPdfPaths(combinedPath, chaptersDir, chapterPaths);
    }

    /// <summary>Writes a combined reference PDF from ordered markdown documents (folder titles as H1).</summary>
    public static string WriteCombined(
        IReadOnlyList<ChapterDocument> chapters,
        string title,
        string? subtitle,
        string pdfPath,
        ManuscriptPrintSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(chapters);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);
        if (chapters.Count == 0)
            throw new InvalidOperationException("No markdown documents to print.");

        settings ??= new ManuscriptPrintSettings();
        var combined = CombineBodies(chapters);
        ManuscriptPagedDocumentBuilder.WriteDocument(
            combined,
            pdfPath,
            new ManuscriptPagedDocumentBuilder.BookCoverMeta(title, subtitle, null, null, null),
            settings,
            includeToc: settings.IncludeToc,
            includeCover: settings.IncludeCover);
        return Path.GetFullPath(pdfPath);
    }

    /// <summary>Concatenates chapter bodies in order into one markdown document.</summary>
    internal static IMarkdownDocument CombineBodies(IReadOnlyList<ChapterDocument> chapters)
    {
        var combined = new MarkdownDocument();
        foreach (var chapter in chapters)
            combined.With(chapter.Body);
        return combined;
    }
}
