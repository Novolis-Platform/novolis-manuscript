using System.Diagnostics.CodeAnalysis;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>
/// Exports books and reference sets to PDF via QuestPDF (books print fidelity).
/// Stable Studio entry points; prefer <see cref="BookPrintExporter"/> for multi-format CLI output.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Studio PDF entry; remodel coverage owns BookPrintExporter + assembler.")]
public static class ManuscriptBookPdfExporter
{
    /// <summary>
    /// Exports an ordered book to a PDF file using QuestPDF (cover, H1 page breaks, chapter-metadata filtering).
    /// </summary>
    /// <param name="book">Catalog book with chapters already ordered.</param>
    /// <param name="outputPath">Destination <c>.pdf</c> path.</param>
    /// <param name="settings">Optional layout/typography settings.</param>
    public static void ExportBook(BookInfo book, string outputPath, ManuscriptPrintSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        settings ??= new ManuscriptPrintSettings();

        var markdown = BookPrintAssembler.AssembleReaderMarkdownFromFiles(
            book.Chapters.Select(c => c.FilePath),
            authorMode: book.DebugMode);
        var yaml = BookYaml.LoadFile(Path.Combine(book.DirectoryPath, "book.yaml"));
        var rights = BookYaml.GetString(yaml, "rights") ?? BookYaml.GetString(yaml, "copyright");
        var series = BookYaml.GetString(yaml, "series")
                     ?? ResolveSeriesTitle(book.DirectoryPath)
                     ?? book.SeriesId;

        QuestPdfDocuments.WriteBookPdf(
            markdown,
            outputPath,
            new QuestPdfDocuments.BookCoverMeta(book.Title, book.Subtitle, series, book.Author, rights),
            showAllTags: book.DebugMode,
            settings);
    }

    /// <summary>
    /// Exports a reference set to a PDF file with cover and table of contents when files are present.
    /// </summary>
    /// <param name="referenceSet">Catalog reference set.</param>
    /// <param name="outputPath">Destination <c>.pdf</c> path.</param>
    /// <param name="settings">Optional layout/typography settings.</param>
    public static void ExportReferenceSet(
        ReferenceSetInfo referenceSet,
        string outputPath,
        ManuscriptPrintSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(referenceSet);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        settings ??= new ManuscriptPrintSettings();

        var toc = new List<QuestPdfDocuments.TocEntry>();
        var body = new System.Text.StringBuilder();
        foreach (var file in referenceSet.Files)
        {
            var fileTitle = string.IsNullOrWhiteSpace(file.Title) ? file.Id : file.Title;
            toc.Add(new QuestPdfDocuments.TocEntry(1, fileTitle));
            if (!File.Exists(file.FilePath))
                continue;
            body.Append(File.ReadAllText(file.FilePath));
            body.AppendLine();
        }

        QuestPdfDocuments.WriteReferencePdf(
            referenceSet.Title,
            coverSubtitle: null,
            toc,
            body.ToString(),
            outputPath,
            settings);
    }

    static string? ResolveSeriesTitle(string bookDirectory)
    {
        var parent = Directory.GetParent(bookDirectory)?.FullName;
        if (parent == null)
            return null;
        var seriesYaml = Path.Combine(parent, "series.yaml");
        if (!File.Exists(seriesYaml))
            return null;
        var yaml = BookYaml.LoadFile(seriesYaml);
        return BookYaml.GetString(yaml, "title") ?? BookYaml.GetString(yaml, "name");
    }
}
