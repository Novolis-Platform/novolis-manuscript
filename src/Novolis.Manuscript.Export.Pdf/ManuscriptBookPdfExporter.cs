using System.Diagnostics.CodeAnalysis;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>
/// Exports books and reference sets to PDF via <c>Novolis.Documents</c> + <c>Documents.Skia</c>.
/// Stable Studio entry points; prefer <see cref="BookPrintExporter"/> for multi-format CLI output.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Studio PDF entry; remodel coverage owns BookPrintExporter + assembler.")]
public static class ManuscriptBookPdfExporter
{
    /// <summary>
    /// Exports an ordered book to a PDF file (cover, chapter headers, chapter-metadata filtering).
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

        ManuscriptPagedDocumentBuilder.WriteBookPdf(
            markdown,
            outputPath,
            new ManuscriptPagedDocumentBuilder.BookCoverMeta(book.Title, book.Subtitle, series, book.Author, rights),
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

        var tocSb = new System.Text.StringBuilder();
        var body = new System.Text.StringBuilder();
        tocSb.AppendLine("# Contents");
        tocSb.AppendLine();
        foreach (var file in referenceSet.Files)
        {
            var fileTitle = string.IsNullOrWhiteSpace(file.Title) ? file.Id : file.Title;
            tocSb.Append("- ").AppendLine(fileTitle);
            if (!File.Exists(file.FilePath))
                continue;
            body.Append(File.ReadAllText(file.FilePath));
            body.AppendLine();
        }

        var fullMd = tocSb + Environment.NewLine + "---" + Environment.NewLine + Environment.NewLine + body;

        ManuscriptPagedDocumentBuilder.WriteReferencePdf(
            referenceSet.Title,
            coverSubtitle: null,
            fullMd,
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
