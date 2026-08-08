using System.Text.RegularExpressions;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Editorial;

/// <summary>Known spelling / naming variants mapped to canonical forms.</summary>
public static class NamingRules
{
    /// <summary>
    /// Calypso cast variants — prefer <see cref="EditorialProfiles.CalypsoNames"/>.
    /// Kept for callers that still reference the old name.
    /// </summary>
    public static IReadOnlyDictionary<string, string> CalypsoCoreNames => EditorialProfiles.CalypsoNames;

    /// <summary>Scans prose for known naming variants (only the supplied map; no built-in cast).</summary>
    public static IReadOnlyList<DiagnosticFinding> Scan(
        string text,
        IReadOnlyDictionary<string, string>? extraNames = null,
        string? path = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (extraNames is not null)
        {
            foreach (var (variant, canonical) in extraNames)
            {
                if (!string.IsNullOrWhiteSpace(variant) && !string.IsNullOrWhiteSpace(canonical))
                    map[variant.Trim()] = canonical.Trim();
            }
        }

        if (map.Count == 0)
            return [];

        var findings = new List<DiagnosticFinding>();
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var variants = map.Keys.OrderByDescending(k => k.Length).ThenBy(k => k, StringComparer.Ordinal).ToList();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNo = i + 1;
            foreach (var variant in variants)
            {
                if (!ContainsWholePhrase(line, variant))
                    continue;
                var canonical = map[variant];
                findings.Add(new DiagnosticFinding(
                    DiagnosticSeverity.Warning,
                    EditorialCodes.NamingVariant,
                    $"Line {lineNo}: naming variant '{variant}' — prefer canonical '{canonical}'.",
                    path));
            }
        }

        return findings;
    }

    static bool ContainsWholePhrase(string line, string phrase)
    {
        var rx = new Regex(
            $@"(?i)(?<![A-Za-z0-9]){Regex.Escape(phrase)}(?![A-Za-z0-9])",
            RegexOptions.CultureInvariant);
        return rx.IsMatch(line);
    }
}
