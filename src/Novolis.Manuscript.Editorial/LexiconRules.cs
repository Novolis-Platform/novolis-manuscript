using System.Text.RegularExpressions;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Editorial;

/// <summary>Forbid-list and prefer-pair lexicon matchers (content packs supplied by caller).</summary>
public static class LexiconRules
{
    /// <summary>Calypso forbid list — prefer <see cref="EditorialProfiles.CalypsoForbiddenPhrases"/>.</summary>
    public static IReadOnlyList<string> ForbiddenPhrases => EditorialProfiles.CalypsoForbiddenPhrases;

    /// <summary>Calypso prefer pairs — prefer <see cref="EditorialProfiles.CalypsoPreferPairs"/>.</summary>
    public static IReadOnlyList<(string Flagged, string Prefer)> PreferPairs => EditorialProfiles.CalypsoPreferPairs;

    /// <summary>Scans prose for forbidden and prefer-pair lexicon hits.</summary>
    public static IReadOnlyList<DiagnosticFinding> Scan(
        string text,
        string? path = null,
        IReadOnlyList<string>? forbiddenPhrases = null,
        IReadOnlyList<(string Flagged, string Prefer)>? preferPairs = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var forbidden = forbiddenPhrases ?? Array.Empty<string>();
        var prefer = preferPairs ?? Array.Empty<(string, string)>();
        if (forbidden.Count == 0 && prefer.Count == 0)
            return [];

        var findings = new List<DiagnosticFinding>();
        var lines = text.Replace("\r\n", "\n").Split('\n');
        Regex? wordBoundaryPhrase = null;
        if (forbidden.Count > 0)
        {
            var alternation = string.Join("|", forbidden
                .OrderByDescending(p => p.Length)
                .ThenBy(p => p, StringComparer.Ordinal)
                .Select(Regex.Escape));
            wordBoundaryPhrase = new Regex(
                $@"(?i)(?<![A-Za-z0-9])(?:{alternation})(?![A-Za-z0-9])",
                RegexOptions.CultureInvariant);
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNo = i + 1;
            if (wordBoundaryPhrase is not null)
            {
                foreach (Match m in wordBoundaryPhrase.Matches(line))
                {
                    findings.Add(new DiagnosticFinding(
                        DiagnosticSeverity.Warning,
                        EditorialCodes.LexiconForbid,
                        $"Line {lineNo}: forbidden lexicon '{m.Value}' (prefer jumpspace / in-universe terms).",
                        path));
                }
            }

            foreach (var (flagged, preferred) in prefer)
            {
                if (ContainsWholeWord(line, flagged))
                {
                    findings.Add(new DiagnosticFinding(
                        DiagnosticSeverity.Info,
                        EditorialCodes.LexiconPrefer,
                        $"Line {lineNo}: prefer '{preferred}' over '{flagged}' in ship/station narrator prose.",
                        path));
                }
            }
        }

        return findings;
    }

    static bool ContainsWholeWord(string line, string word)
    {
        var rx = new Regex($@"(?i)(?<![A-Za-z0-9]){Regex.Escape(word)}(?![A-Za-z0-9])",
            RegexOptions.CultureInvariant);
        return rx.IsMatch(line);
    }
}
