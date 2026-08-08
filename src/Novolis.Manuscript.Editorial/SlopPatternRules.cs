using System.Text.RegularExpressions;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Editorial;

/// <summary>Deterministic AI-slop pattern detectors from editorial guidelines.</summary>
public static class SlopPatternRules
{
    // Not X. Y.  / Not X. Not Y. Just Z.  — sentence-initial Not …
    static readonly Regex CorrelativeNegation = new(
        @"^\s*Not\s+[^.\n]{1,80}\.\s+(?:Not\s+[^.\n]{1,80}\.\s+)?(?:Just\s+)?[A-Z""']",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    static readonly Regex AnswerAsQuestion = new(
        @"\bThe\s+(?:result|solution|answer|truth|key)\?\s+[A-Z]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    static readonly Regex UnearnedProfundity = new(
        @"^\s*(?:Something shifted|Everything changed|But here's the thing|But here is the thing)\.\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    static readonly Regex NatureOfAbstract = new(
        @"\b(?:Such was the nature of|the nature of (?:power|fear|violence|authority|trust|law|politics|war|money))\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    static readonly Regex SenseOfEmotion = new(
        @"\ba sense of (?:dread|foreboding|unease|excitement)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    static readonly Regex HowSystemsWork = new(
        @"\b(?:what fear does|how (?:power|systems|violence) work)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Scans prose for known slop patterns.</summary>
    public static IReadOnlyList<DiagnosticFinding> Scan(string text, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var findings = new List<DiagnosticFinding>();
        var normalized = text.Replace("\r\n", "\n");

        foreach (Match m in CorrelativeNegation.Matches(normalized))
        {
            var lineNo = LineNumberAt(normalized, m.Index);
            var snippet = Truncate(m.Value.Trim(), 72);
            findings.Add(new DiagnosticFinding(
                DiagnosticSeverity.Warning,
                EditorialCodes.SlopCorrelativeNegation,
                $"Line {lineNo}: correlative negation pattern ({snippet}). Prefer direct observation.",
                path));
        }

        foreach (Match m in AnswerAsQuestion.Matches(normalized))
        {
            var lineNo = LineNumberAt(normalized, m.Index);
            findings.Add(new DiagnosticFinding(
                DiagnosticSeverity.Warning,
                EditorialCodes.SlopAnswerAsQuestion,
                $"Line {lineNo}: answer-as-question fragment ('{Truncate(m.Value.Trim(), 48)}').",
                path));
        }

        foreach (Match m in UnearnedProfundity.Matches(normalized))
        {
            var lineNo = LineNumberAt(normalized, m.Index);
            findings.Add(new DiagnosticFinding(
                DiagnosticSeverity.Warning,
                EditorialCodes.SlopUnearnedProfundity,
                $"Line {lineNo}: unearned profundity beat ('{m.Value.Trim()}'). Anchor or cut.",
                path));
        }

        AddSimple(findings, NatureOfAbstract.Matches(normalized), normalized, path,
            EditorialCodes.SlopUnearnedProfundity, "abstract 'nature of' commentary");
        AddSimple(findings, SenseOfEmotion.Matches(normalized), normalized, path,
            EditorialCodes.SlopUnearnedProfundity, "labeled 'a sense of [emotion]'");
        AddSimple(findings, HowSystemsWork.Matches(normalized), normalized, path,
            EditorialCodes.SlopUnearnedProfundity, "how-systems-work commentary");

        return findings;
    }

    static void AddSimple(
        List<DiagnosticFinding> findings,
        MatchCollection matches,
        string normalized,
        string? path,
        string code,
        string label)
    {
        foreach (Match m in matches)
        {
            var lineNo = LineNumberAt(normalized, m.Index);
            findings.Add(new DiagnosticFinding(
                DiagnosticSeverity.Warning,
                code,
                $"Line {lineNo}: {label} ('{Truncate(m.Value.Trim(), 48)}').",
                path));
        }
    }

    static int LineNumberAt(string text, int index)
    {
        var line = 1;
        for (var i = 0; i < index && i < text.Length; i++)
        {
            if (text[i] == '\n')
                line++;
        }

        return line;
    }

    static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";
}
