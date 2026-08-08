using Novolis.Manuscript;

namespace Novolis.Manuscript.Editorial;

/// <summary>Runs deterministic editorial detectors over chapter prose.</summary>
public static class EditorialAnalyzer
{
    /// <summary>Analyzes a single markdown document (metadata stripped for body scan).</summary>
    public static IReadOnlyList<DiagnosticFinding> AnalyzeText(
        string markdown,
        EditorialOptions? options = null,
        string? path = null)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        options ??= new EditorialOptions();
        var body = ManuscriptMetadata.GetBodyForWordCount(markdown);
        var findings = new List<DiagnosticFinding>();

        if (options.LexiconEnabled)
            findings.AddRange(LexiconRules.Scan(
                body,
                path,
                options.ForbiddenPhrases,
                options.PreferPairs));
        if (options.EnableSlop)
            findings.AddRange(SlopPatternRules.Scan(body, path));
        if (options.EnableNaming)
            findings.AddRange(NamingRules.Scan(body, options.ExtraNames, path));

        return findings;
    }

    /// <summary>Scans all <c>*.md</c> files in a chapters directory (top-level only).</summary>
    public static IReadOnlyList<DiagnosticFinding> AnalyzeChaptersDir(
        string chaptersDir,
        EditorialOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chaptersDir);
        options ??= new EditorialOptions();
        if (!Directory.Exists(chaptersDir))
            throw new DirectoryNotFoundException(chaptersDir);

        var findings = new List<DiagnosticFinding>();
        foreach (var path in Directory.GetFiles(chaptersDir, "*.md", SearchOption.TopDirectoryOnly)
                     .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var text = File.ReadAllText(path);
            findings.AddRange(AnalyzeText(text, options, path));
        }

        return findings;
    }
}
