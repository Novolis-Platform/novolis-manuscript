using System.Diagnostics.CodeAnalysis;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Exports series reference manuals with a cover page. Contents come from layout, not a markdown list.</summary>
[ExcludeFromCodeCoverage]
public static class ReferenceManualExporter
{
    /// <summary>
    /// Exports all Markdown under <paramref name="referencesDirectory"/> to
    /// <c>reference.md</c> / <c>.html</c> / <c>.txt</c> / <c>.pdf</c> in <paramref name="outputDirectory"/>.
    /// </summary>
    /// <param name="referencesDirectory">Root folder of reference Markdown (scanned recursively).</param>
    /// <param name="outputDirectory">Destination folder.</param>
    /// <param name="seriesId">Series id used for cover subtitle when <paramref name="coverSubtitle"/> is null.</param>
    /// <param name="title">Cover / document title (e.g. <c>Reference Manual</c>).</param>
    /// <param name="coverSubtitle">Optional cover subtitle; defaults to a title-cased <paramref name="seriesId"/>.</param>
    /// <param name="settings">Optional print settings.</param>
    /// <param name="fileStem">Output file stem (default <c>reference</c>).</param>
    public static ReferencePrintPaths Export(
        string referencesDirectory,
        string outputDirectory,
        string seriesId,
        string title,
        string? coverSubtitle = null,
        ManuscriptPrintSettings? settings = null,
        string fileStem = "reference")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referencesDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileStem);
        settings ??= new ManuscriptPrintSettings();

        if (!Directory.Exists(referencesDirectory))
            throw new DirectoryNotFoundException($"References directory not found: {referencesDirectory}");

        var files = Directory.GetFiles(referencesDirectory, "*.md", SearchOption.AllDirectories)
            .OrderBy(f => Path.GetRelativePath(referencesDirectory, f), StringComparer.Ordinal)
            .ToList();
        if (files.Count == 0)
            throw new InvalidOperationException($"No Markdown files under {referencesDirectory}");

        coverSubtitle ??= ReferenceMarkdown.ToTitleCaseWords(seriesId);
        return WriteSet(files, referencesDirectory, outputDirectory, fileStem, title, coverSubtitle, settings);
    }

    /// <summary>Exports a catalog <see cref="ReferenceSetInfo"/> to multi-format artifacts.</summary>
    public static ReferencePrintPaths ExportSet(
        ReferenceSetInfo referenceSet,
        string outputDirectory,
        string? seriesId = null,
        ManuscriptPrintSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(referenceSet);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        settings ??= new ManuscriptPrintSettings();

        if (referenceSet.Files.Count == 0)
            throw new InvalidOperationException($"Reference set '{referenceSet.Id}' has no files.");

        var files = referenceSet.Files.Select(f => f.FilePath).ToList();
        var coverSubtitle = ReferenceMarkdown.ToTitleCaseWords(seriesId ?? referenceSet.Id);
        return WriteSet(
            files,
            referenceSet.DirectoryPath,
            outputDirectory,
            referenceSet.Id,
            referenceSet.Title,
            coverSubtitle,
            settings);
    }

    static ReferencePrintPaths WriteSet(
        IReadOnlyList<string> files,
        string contentRoot,
        string outputDirectory,
        string fileStem,
        string title,
        string? subtitle,
        ManuscriptPrintSettings settings)
    {
        var chapters = ReferenceMarkdown.LoadFiles(files, contentRoot);
        var fullMd = BookPdfWriter.CombineBodies(chapters).ToString();

        Directory.CreateDirectory(outputDirectory);
        var stem = Path.Combine(outputDirectory, fileStem);
        var mdPath = stem + ".md";
        var htmlPath = stem + ".html";
        var txtPath = stem + ".txt";
        var pdfPath = stem + ".pdf";

        ManuscriptDocumentEmitters.WriteMarkdown(fullMd, mdPath);
        var css = StylesheetLocator.Find(contentRoot);
        ManuscriptDocumentEmitters.WriteHtml(title, fullMd, htmlPath, css, showAllTags: false);
        ManuscriptDocumentEmitters.WritePlainText(fullMd, txtPath, showAllTags: false);
        BookPdfWriter.WriteCombined(chapters, title, subtitle, pdfPath, settings);

        return new ReferencePrintPaths(
            Path.GetFullPath(mdPath),
            Path.GetFullPath(htmlPath),
            Path.GetFullPath(txtPath),
            Path.GetFullPath(pdfPath));
    }
}
