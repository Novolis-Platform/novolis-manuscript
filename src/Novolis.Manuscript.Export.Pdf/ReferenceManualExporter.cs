using System.Text;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Paths written by a reference-manual print export.</summary>
/// <param name="MarkdownPath">Combined reference Markdown (includes TOC).</param>
/// <param name="HtmlPath">HTML companion.</param>
/// <param name="TextPath">Plain-text companion.</param>
/// <param name="PdfPath">QuestPDF output with cover and TOC.</param>
public sealed record ReferencePrintPaths(
    string MarkdownPath,
    string HtmlPath,
    string TextPath,
    string PdfPath);

/// <summary>Exports series reference manuals with QuestPDF cover + table of contents.</summary>
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

        var inputs = files.Select(f => new RefSource(f, referencesDirectory)).ToList();
        var (fullMd, bodyMd, toc) = BuildCombinedMarkdown(inputs);
        coverSubtitle ??= ToTitleCaseWords(seriesId);

        Directory.CreateDirectory(outputDirectory);
        var stem = Path.Combine(outputDirectory, fileStem);
        var mdPath = stem + ".md";
        var htmlPath = stem + ".html";
        var txtPath = stem + ".txt";
        var pdfPath = stem + ".pdf";

        ManuscriptDocumentEmitters.WriteMarkdown(fullMd, mdPath);
        var css = StylesheetLocator.Find(referencesDirectory);
        ManuscriptDocumentEmitters.WriteHtml(title, fullMd, htmlPath, css, showAllTags: false);
        ManuscriptDocumentEmitters.WritePlainText(fullMd, txtPath, showAllTags: false);
        QuestPdfDocuments.WriteReferencePdf(title, coverSubtitle, toc, bodyMd, pdfPath, settings);

        return new ReferencePrintPaths(
            Path.GetFullPath(mdPath),
            Path.GetFullPath(htmlPath),
            Path.GetFullPath(txtPath),
            Path.GetFullPath(pdfPath));
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

        var contentRoot = referenceSet.DirectoryPath;
        var inputs = referenceSet.Files
            .Select(f => new RefSource(f.FilePath, contentRoot))
            .ToList();
        var (fullMd, bodyMd, toc) = BuildCombinedMarkdown(inputs);
        var coverSubtitle = ToTitleCaseWords(seriesId ?? referenceSet.Id);

        Directory.CreateDirectory(outputDirectory);
        var stem = Path.Combine(outputDirectory, referenceSet.Id);
        var mdPath = stem + ".md";
        var htmlPath = stem + ".html";
        var txtPath = stem + ".txt";
        var pdfPath = stem + ".pdf";

        ManuscriptDocumentEmitters.WriteMarkdown(fullMd, mdPath);
        var css = StylesheetLocator.Find(referenceSet.DirectoryPath);
        ManuscriptDocumentEmitters.WriteHtml(referenceSet.Title, fullMd, htmlPath, css, showAllTags: false);
        ManuscriptDocumentEmitters.WritePlainText(fullMd, txtPath, showAllTags: false);
        QuestPdfDocuments.WriteReferencePdf(referenceSet.Title, coverSubtitle, toc, bodyMd, pdfPath, settings);

        return new ReferencePrintPaths(
            Path.GetFullPath(mdPath),
            Path.GetFullPath(htmlPath),
            Path.GetFullPath(txtPath),
            Path.GetFullPath(pdfPath));
    }

    readonly record struct RefSource(string AbsolutePath, string ContentRoot);

    static (string fullMd, string bodyMd, List<QuestPdfDocuments.TocEntry> toc) BuildCombinedMarkdown(
        IReadOnlyList<RefSource> refInputs)
    {
        var toc = new List<QuestPdfDocuments.TocEntry>();
        var body = new StringBuilder();
        string? prevContentRoot = null;
        var prevRelParts = new List<string>();

        foreach (var rsf in refInputs)
        {
            if (!string.Equals(prevContentRoot, rsf.ContentRoot, StringComparison.OrdinalIgnoreCase))
            {
                prevContentRoot = rsf.ContentRoot;
                prevRelParts = [];
            }

            var relToContent = Path.GetRelativePath(rsf.ContentRoot, rsf.AbsolutePath);
            var dir = Path.GetDirectoryName(relToContent);
            var parts = string.IsNullOrEmpty(dir)
                ? new List<string>()
                : dir.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries).ToList();

            var diverge = 0;
            var minLen = Math.Min(parts.Count, prevRelParts.Count);
            while (diverge < minLen && string.Equals(parts[diverge], prevRelParts[diverge], StringComparison.OrdinalIgnoreCase))
                diverge++;

            for (var i = diverge; i < parts.Count; i++)
            {
                var subTitle = ToTitleCaseWords(parts[i]);
                var level = Math.Min(i + 2, 6);
                body.AppendLine($"{new string('#', level)} {subTitle}");
                body.AppendLine();
                toc.Add(new QuestPdfDocuments.TocEntry(level, subTitle));
            }

            prevRelParts = [.. parts];

            var fileTitle = ToTitleCaseWords(Path.GetFileNameWithoutExtension(rsf.AbsolutePath));
            var fileLevel = Math.Min(parts.Count + 2, 6);
            toc.Add(new QuestPdfDocuments.TocEntry(fileLevel, fileTitle));

            if (File.Exists(rsf.AbsolutePath))
            {
                body.Append(File.ReadAllText(rsf.AbsolutePath));
                body.AppendLine();
            }
        }

        var bodyMd = body.ToString();
        var tocSb = new StringBuilder();
        tocSb.AppendLine("# Contents");
        tocSb.AppendLine();
        foreach (var e in toc)
        {
            var padLen = Math.Max(0, (e.Level - 1) * 2);
            tocSb.Append(' ', padLen).Append("- ").AppendLine(e.Title);
        }

        var fullMd = tocSb + Environment.NewLine + "---" + Environment.NewLine + Environment.NewLine + bodyMd;
        return (fullMd, bodyMd, toc);
    }

    static string ToTitleCaseWords(string? folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return "";
        return string.Join(" ", folderName.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Length > 0 ? char.ToUpperInvariant(s[0]) + s[1..] : s));
    }
}
