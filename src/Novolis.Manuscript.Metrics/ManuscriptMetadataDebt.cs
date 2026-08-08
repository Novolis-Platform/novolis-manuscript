using Novolis.Manuscript;

namespace Novolis.Manuscript.Metrics;

/// <summary>Stable finding codes for chapter metadata quality (not structural Doctor).</summary>
public static class MetadataDebtCodes
{
    /// <summary>Literal <c>TK</c> placeholder in a metadata field.</summary>
    public const string MetadataTk = "metadata-tk";

    /// <summary>Chapter has metadata block but missing <c>pov</c>.</summary>
    public const string MissingPov = "metadata-missing-pov";

    /// <summary>Chapter has metadata block but missing <c>characters</c>.</summary>
    public const string MissingCharacters = "metadata-missing-characters";
}

/// <summary>Chapter metadata quality / TK debt diagnostics.</summary>
public static class ManuscriptMetadataDebt
{
    /// <summary>
    /// Diagnoses metadata TK placeholders and missing pov/characters when a metadata format is present.
    /// Does not flag empty chapter bodies (structural Doctor owns that).
    /// </summary>
    public static IReadOnlyList<DiagnosticFinding> Diagnose(string chaptersDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chaptersDir);
        if (!Directory.Exists(chaptersDir))
            throw new DirectoryNotFoundException(chaptersDir);

        var findings = new List<DiagnosticFinding>();
        foreach (var path in Directory.GetFiles(chaptersDir, "*.md", SearchOption.TopDirectoryOnly)
                     .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var raw = File.ReadAllText(path);
            var (meta, _, format) = ManuscriptMetadata.Parse(raw);
            if (format == ManuscriptMetadataFormat.None)
                continue;

            findings.AddRange(DiagnoseMeta(meta, path));
        }

        return findings;
    }

    /// <summary>Diagnoses a single parsed metadata block.</summary>
    public static IReadOnlyList<DiagnosticFinding> DiagnoseMeta(
        ManuscriptChapterMetadata meta,
        string? path = null)
    {
        ArgumentNullException.ThrowIfNull(meta);
        var findings = new List<DiagnosticFinding>();

        void CheckTk(string field, string? value)
        {
            if (IsTk(value))
            {
                findings.Add(new DiagnosticFinding(
                    DiagnosticSeverity.Warning,
                    MetadataDebtCodes.MetadataTk,
                    $"Metadata field '{field}' is still TK.",
                    path));
            }
        }

        CheckTk("date", meta.Date);
        CheckTk("time", meta.Time);
        CheckTk("system", meta.System);
        CheckTk("location", meta.Location);
        CheckTk("pov", meta.Pov);
        CheckTk("characters", meta.Characters);
        CheckTk("status", meta.Status);
        CheckTk("notes", meta.Notes);
        foreach (var kv in meta.Extra)
            CheckTk(kv.Key, kv.Value);

        if (string.IsNullOrWhiteSpace(meta.Pov) || IsTk(meta.Pov))
        {
            // Missing pov: only when completely absent (TK already reported above).
            if (string.IsNullOrWhiteSpace(meta.Pov))
            {
                findings.Add(new DiagnosticFinding(
                    DiagnosticSeverity.Info,
                    MetadataDebtCodes.MissingPov,
                    "Chapter metadata is present but pov is missing.",
                    path));
            }
        }

        if (string.IsNullOrWhiteSpace(meta.Characters))
        {
            findings.Add(new DiagnosticFinding(
                DiagnosticSeverity.Info,
                MetadataDebtCodes.MissingCharacters,
                "Chapter metadata is present but characters is missing.",
                path));
        }

        return findings;
    }

    static bool IsTk(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return string.Equals(value.Trim(), "TK", StringComparison.OrdinalIgnoreCase);
    }
}
