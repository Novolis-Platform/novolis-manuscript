using System.Text;

namespace Novolis.Manuscript;

/// <summary>One non-ASCII codepoint remaining after known replacements (or during scan).</summary>
public sealed record AsciiIssue(string Path, int Line, int Column, int Codepoint, int Index);

/// <summary>Result of normalizing one file or string to ASCII house style.</summary>
public sealed record AsciiNormalizeResult(
    string Text,
    int Replacements,
    bool HasRemainingNonAscii,
    IReadOnlyList<AsciiIssue> RemainingIssues);

/// <summary>
/// House-style ASCII normalization for manuscript Markdown:
/// em/en dash → '-', curly quotes → straight, ellipsis → '...', NBSP → space,
/// strip leading BOM and zero-width characters.
/// </summary>
public static class ManuscriptAscii
{
    /// <summary>Replaces known Unicode punctuation / invisible characters with ASCII equivalents.</summary>
    public static AsciiNormalizeResult Normalize(string text, string? pathForIssues = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var count = 0;
        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            text = text[1..];
            count++;
        }

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            switch (c)
            {
                case '\u2014':
                case '\u2013':
                    sb.Append('-');
                    count++;
                    break;
                case '\u201C':
                case '\u201D':
                    sb.Append('"');
                    count++;
                    break;
                case '\u2018':
                case '\u2019':
                    sb.Append('\'');
                    count++;
                    break;
                case '\u2026':
                    sb.Append("...");
                    count++;
                    break;
                case '\u00A0':
                    sb.Append(' ');
                    count++;
                    break;
                case '\u200B':
                case '\u200C':
                case '\u200D':
                case '\uFEFF':
                    count++;
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        var replaced = sb.ToString();
        var issues = FindNonAscii(replaced, pathForIssues ?? "", limit: 32);
        return new AsciiNormalizeResult(replaced, count, issues.Count > 0, issues);
    }

    /// <summary>Scans text for non-ASCII / control characters (excluding tab/CR/LF).</summary>
    public static IReadOnlyList<AsciiIssue> Scan(string text, string path = "", int limit = 100)
    {
        ArgumentNullException.ThrowIfNull(text);
        var issues = new List<AsciiIssue>();
        var line = 1;
        var col = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            col++;
            if (c == '\n')
            {
                line++;
                col = 0;
                continue;
            }

            if (c is '\r' or '\t')
                continue;
            if (c < 32 || c > 126)
            {
                issues.Add(new AsciiIssue(path, line, col, c, i));
                if (issues.Count >= limit)
                    break;
            }
        }

        return issues;
    }

    /// <summary>Normalizes a Markdown file in place (or dry-run).</summary>
    public static AsciiNormalizeResult NormalizeFile(string path, bool dryRun, bool relax)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("File not found.", path);

        var original = File.ReadAllText(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false));
        var result = Normalize(original, path);
        if (result.Replacements == 0 && result.Text == original)
            return result;

        if (!relax && result.HasRemainingNonAscii)
            return result;

        if (!dryRun)
            File.WriteAllText(path, result.Text, new UTF8Encoding(false));

        return result;
    }

    /// <summary>Normalizes every <c>*.md</c> in a chapters directory.</summary>
    public static IReadOnlyList<(string Path, AsciiNormalizeResult Result)> NormalizeChaptersDirectory(
        string chaptersDir,
        bool dryRun,
        bool relax)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chaptersDir);
        if (!Directory.Exists(chaptersDir))
            throw new DirectoryNotFoundException(chaptersDir);

        var rows = new List<(string, AsciiNormalizeResult)>();
        foreach (var file in Directory.GetFiles(chaptersDir, "*.md", SearchOption.TopDirectoryOnly)
                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            rows.Add((file, NormalizeFile(file, dryRun, relax)));
        }

        return rows;
    }

    static IReadOnlyList<AsciiIssue> FindNonAscii(string text, string path, int limit)
    {
        var issues = new List<AsciiIssue>();
        var line = 1;
        var col = 0;
        for (var i = 0; i < text.Length; i++)
        {
            col++;
            if (text[i] == '\n')
            {
                line++;
                col = 0;
                continue;
            }

            if (text[i] > '\u007F')
            {
                issues.Add(new AsciiIssue(path, line, col, text[i], i));
                if (issues.Count >= limit)
                    break;
            }
        }

        return issues;
    }
}
