using System.Text.RegularExpressions;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Editorial;

/// <summary>Forbid-list and prefer-pair lexicon matchers (Calypso / fiction defaults).</summary>
public static class LexiconRules
{
    /// <summary>Wrong-universe / wrong-tech phrases (longest first).</summary>
    public static readonly IReadOnlyList<string> ForbiddenPhrases =
    [
        "warp drive", "warp core", "warp factor", "warp bubble",
        "hyperdrive core", "hyperspace lane", "photon torpedo", "photon torpedoes",
        "quantum torpedo", "quantum torpedoes", "quantum drive", "quantum leap",
        "beam up", "beam out", "dilithium chamber", "impulse drive", "deflector dish",
        "tractor beam", "ludicrous speed", "lightspeed drive", "AI singularity",
        "laser pistol", "ray gun",
        "warp", "hyperspace", "hyperspeed", "hyperdrive", "subspace",
        "phaser", "phasers", "transporter", "dilithium", "replicator", "holodeck",
        "Starfleet", "Federation", "blaster", "hypersleep",
    ];

    /// <summary>Ship/station prefer pairs (flagged term → preferred).</summary>
    public static readonly IReadOnlyList<(string Flagged, string Prefer)> PreferPairs =
    [
        ("hallway", "corridor"),
        ("ceiling", "overhead"),
    ];

    static readonly Regex WordBoundaryPhrase;

    static LexiconRules()
    {
        // Longest phrases first so "warp drive" wins over "warp".
        var alternation = string.Join("|", ForbiddenPhrases
            .OrderByDescending(p => p.Length)
            .ThenBy(p => p, StringComparer.Ordinal)
            .Select(Regex.Escape));
        WordBoundaryPhrase = new Regex(
            $@"(?i)(?<![A-Za-z0-9])(?:{alternation})(?![A-Za-z0-9])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    /// <summary>Scans prose for forbidden and prefer-pair lexicon hits.</summary>
    public static IReadOnlyList<DiagnosticFinding> Scan(string text, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var findings = new List<DiagnosticFinding>();
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNo = i + 1;
            foreach (Match m in WordBoundaryPhrase.Matches(line))
            {
                var hit = m.Value;
                // Allowed homonym: "warped" is not in the list; whole-word "warp" is.
                findings.Add(new DiagnosticFinding(
                    DiagnosticSeverity.Warning,
                    EditorialCodes.LexiconForbid,
                    $"Line {lineNo}: forbidden lexicon '{hit}' (prefer jumpspace / in-universe terms).",
                    path));
            }

            foreach (var (flagged, prefer) in PreferPairs)
            {
                if (ContainsWholeWord(line, flagged))
                {
                    findings.Add(new DiagnosticFinding(
                        DiagnosticSeverity.Info,
                        EditorialCodes.LexiconPrefer,
                        $"Line {lineNo}: prefer '{prefer}' over '{flagged}' in ship/station narrator prose.",
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
