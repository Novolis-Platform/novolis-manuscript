using System.Text;
using Novolis.Markup.Markdown.Rendering;

using Novolis.Manuscript;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Exports books and reference sets to PDF via Markdown rendering.</summary>
public static class ManuscriptBookPdfExporter
{
    /// <summary>Exports an ordered book to a PDF file.</summary>
    public static void ExportBook(BookInfo book, string outputPath, ManuscriptPrintSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        settings ??= new ManuscriptPrintSettings();

        var markdown = ConcatenateDocuments(book.Chapters.Select(c => c.FilePath));
        var options = settings.ToPdfOptions(book.Title, book.Subtitle, book.Author);
        MarkdownPdfExporter.ExportToFile(markdown, outputPath, options);
    }

    /// <summary>Exports a reference set to a PDF file.</summary>
    public static void ExportReferenceSet(ReferenceSetInfo referenceSet, string outputPath, ManuscriptPrintSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(referenceSet);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        settings ??= new ManuscriptPrintSettings();

        var markdown = ConcatenateDocuments(referenceSet.Files.Select(f => f.FilePath));
        var options = settings.ToPdfOptions(referenceSet.Title, subtitle: null, author: null);
        MarkdownPdfExporter.ExportToFile(markdown, outputPath, options);
    }

    static string ConcatenateDocuments(IEnumerable<string> paths)
    {
        var sb = new StringBuilder();
        var first = true;
        foreach (var path in paths)
        {
            if (!File.Exists(path))
                continue;

            var body = File.ReadAllText(path);
            if (body.StartsWith('\uFEFF'))
                body = body[1..];

            if (!first)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            sb.AppendLine(body.TrimEnd());
            first = false;
        }

        return sb.ToString();
    }
}
