using System.Diagnostics.CodeAnalysis;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>
/// Studio PDF entry points. Delegates to <see cref="BookPdfWriter"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Studio PDF entry; BookPdfWriter owns print coverage.")]
public static class ManuscriptBookPdfExporter
{
    /// <summary>
    /// Exports an ordered book to a PDF file (cover, contents, chapter headers).
    /// </summary>
    public static void ExportBook(BookInfo book, string outputPath, ManuscriptPrintSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        var document = BookDocument.Open(book.DirectoryPath);
        var dir = Path.GetDirectoryName(Path.GetFullPath(outputPath))!;
        var stem = Path.GetFileNameWithoutExtension(outputPath);
        BookPdfWriter.Write(document, dir, stem, BookPdfOutput.Combine, settings);
    }

    /// <summary>
    /// Exports a reference set to a PDF file. Folder titles become level-1 headings so layout fills contents.
    /// </summary>
    public static void ExportReferenceSet(
        ReferenceSetInfo referenceSet,
        string outputPath,
        ManuscriptPrintSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(referenceSet);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (referenceSet.Files.Count == 0)
            throw new InvalidOperationException($"Reference set '{referenceSet.Id}' has no files.");

        settings ??= new ManuscriptPrintSettings();
        var chapters = ReferenceMarkdown.LoadFiles(
            referenceSet.Files.Select(f => f.FilePath),
            referenceSet.DirectoryPath);
        BookPdfWriter.WriteCombined(chapters, referenceSet.Title, subtitle: null, outputPath, settings);
    }
}
