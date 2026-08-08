namespace Novolis.Manuscript;

/// <summary>Severity of a diagnostic finding.</summary>
public enum DiagnosticSeverity
{
    /// <summary>Informational note.</summary>
    Info,

    /// <summary>Non-fatal issue.</summary>
    Warning,

    /// <summary>Blocking structural problem.</summary>
    Error,
}

/// <summary>One manuscript doctor finding.</summary>
public sealed record DiagnosticFinding(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Path = null);

/// <summary>Structural diagnostics for series/book trees (replaces CLI doctor).</summary>
public static class ManuscriptDoctor
{
    /// <summary>Diagnoses an entire content root.</summary>
    public static IReadOnlyList<DiagnosticFinding> Diagnose(string contentRoot)
    {
        var catalog = new ManuscriptCatalog();
        var findings = new List<DiagnosticFinding>();
        foreach (var series in catalog.Load(contentRoot))
            findings.AddRange(Diagnose(series));
        foreach (var book in catalog.LoadStandaloneBooks(contentRoot))
            findings.AddRange(Diagnose(book));
        return findings;
    }

    /// <summary>Diagnoses a series and its books.</summary>
    public static IReadOnlyList<DiagnosticFinding> Diagnose(SeriesInfo series)
    {
        var findings = new List<DiagnosticFinding>();
        var seriesYaml = Path.Combine(series.DirectoryPath, "series.yaml");
        if (!File.Exists(seriesYaml))
            findings.Add(new DiagnosticFinding(DiagnosticSeverity.Warning, "missing-series-yaml", "series.yaml is missing.", seriesYaml));

        foreach (var book in series.Books)
            findings.AddRange(Diagnose(book));

        foreach (var set in series.References)
        {
            if (set.Files.Count == 0)
                findings.Add(new DiagnosticFinding(DiagnosticSeverity.Info, "empty-reference-set", $"Reference set '{set.Id}' has no markdown files.", set.DirectoryPath));
        }

        return findings;
    }

    /// <summary>Diagnoses a single book.</summary>
    public static IReadOnlyList<DiagnosticFinding> Diagnose(BookInfo book)
    {
        var findings = new List<DiagnosticFinding>();
        var bookYaml = Path.Combine(book.DirectoryPath, "book.yaml");
        if (!File.Exists(bookYaml))
            findings.Add(new DiagnosticFinding(DiagnosticSeverity.Error, "missing-book-yaml", "book.yaml is missing.", bookYaml));

        if (book.Chapters.Count == 0)
            findings.Add(new DiagnosticFinding(DiagnosticSeverity.Warning, "no-chapters", "Book has no chapters or appendices.", book.DirectoryPath));

        var stems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var chapter in book.Chapters)
        {
            if (!stems.Add(chapter.Id))
                findings.Add(new DiagnosticFinding(DiagnosticSeverity.Error, "duplicate-chapter-stem", $"Duplicate chapter id '{chapter.Id}'.", chapter.FilePath));

            if (!File.Exists(chapter.FilePath))
            {
                findings.Add(new DiagnosticFinding(DiagnosticSeverity.Error, "missing-chapter-file", "Chapter file is missing.", chapter.FilePath));
                continue;
            }

            string text;
            try
            {
                text = File.ReadAllText(chapter.FilePath);
            }
            catch (Exception ex)
            {
                findings.Add(new DiagnosticFinding(DiagnosticSeverity.Error, "unreadable-chapter", $"Cannot read chapter: {ex.Message}", chapter.FilePath));
                continue;
            }

            if (string.IsNullOrWhiteSpace(text))
                findings.Add(new DiagnosticFinding(DiagnosticSeverity.Warning, "empty-chapter", "Chapter file is empty.", chapter.FilePath));

            var headingTitle = ChapterOrder.ReadChapterTitle(chapter.FilePath);
            if (!string.IsNullOrWhiteSpace(headingTitle)
                && !string.Equals(headingTitle, chapter.Title, StringComparison.Ordinal)
                && !string.Equals(headingTitle, chapter.Id, StringComparison.OrdinalIgnoreCase))
            {
                // Title already comes from heading; mismatch only if catalog was stale — skip noisy check.
            }

            if (book.ChapterOrderFromHeading
                && chapter.Kind == ChapterKind.Chapter
                && double.IsPositiveInfinity(chapter.SortKey))
            {
                findings.Add(new DiagnosticFinding(
                    DiagnosticSeverity.Warning,
                    "missing-chapter-order",
                    "chapter_order_from_heading is set but no chapter number was found.",
                    chapter.FilePath));
            }
        }

        // Orphan-ish: reference roots with no files already handled; flag reference files outside sets is N/A.

        return findings;
    }
}
