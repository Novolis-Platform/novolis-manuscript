using System.Text.RegularExpressions;
using Novolis.Markup.Markdown;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Blockquotes for chapter datelines: legacy <c>[!tag] value</c> or plain public mirrors.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Legacy callout quote parsing; reader path uses assembler datelines.")]
internal static class ChapterMetadataQuote
{
    static readonly Regex TagOpenings = new(@"\[!([a-z0-9_-]+)\]\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex PublicDatelineValueRegex = new(
        @"^(\d{4}\.\d{1,4}(?:\s+\d{1,2}:\d{2})?|\d{4}-\d{2}-\d{2}(?:\s+\d{1,2}:\d{2})?|TK|TBD)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Extract every <c>[!tag] value</c> pair from plain text. Hand-authored files sometimes merge
    /// several callout lines into one paragraph, so one line may contain <c>[!date] ... [!time] ...</c>.
    /// </summary>
    public static List<(string Tag, string Value)> SplitFieldsFromPlain(string plain)
    {
        var list = new List<(string, string)>();
        plain = plain.Trim();
        if (plain.Length == 0)
            return list;
        var matches = TagOpenings.Matches(plain);
        if (matches.Count == 0 || matches[0].Index != 0)
            return list;
        for (var i = 0; i < matches.Count; i++)
        {
            var tag = matches[i].Groups[1].Value.ToLowerInvariant();
            var start = matches[i].Index + matches[i].Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : plain.Length;
            var val = plain.Substring(start, end - start).Trim();
            if (val.Length > 0)
                list.Add((tag, val));
        }

        return list;
    }

    /// <summary>
    /// Extracts dateline rows from a quote/alert. Plain (untagged) lines are only accepted when
    /// <paramref name="blockAlreadyStarted"/> or the text is a stardate / TK public mirror.
    /// </summary>
    public static bool TryGetRows(
        IMarkdownSection section,
        bool blockAlreadyStarted,
        out List<(string Tag, string Value)> rows)
    {
        var text = section switch
        {
            IMarkdownAlert alert => string.Join(' ', alert.Text).Trim(),
            IMarkdownQuote quote => string.Join(' ', quote.Text).Trim(),
            _ => null,
        };
        if (text is null)
        {
            rows = [];
            return false;
        }

        if (text.Length == 0)
        {
            rows = [];
            return blockAlreadyStarted;
        }

        rows = SplitFieldsFromPlain(text);
        if (rows.Count > 0)
            return true;

        if (!blockAlreadyStarted && !PublicDatelineValueRegex.IsMatch(text))
        {
            rows = [];
            return false;
        }

        rows = [("line", text)];
        return true;
    }
}

/// <summary>Which <c>[!tag]</c> lines appear in reader-facing builds.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Thin delegate to ChapterMetadataVisibility.")]
internal static class ChapterMetadataTagVisibility
{
    public static bool IsPublicTag(string tag) =>
        string.Equals(tag, "line", StringComparison.OrdinalIgnoreCase)
        || Novolis.Manuscript.ChapterMetadataVisibility.IsPublicTag(tag);

    public static List<(string Tag, string Value)> FilterForBuild(
        List<(string Tag, string Value)> rows,
        bool showAllTags)
    {
        IEnumerable<(string Tag, string Value)> q = rows.Where(r => !string.IsNullOrWhiteSpace(r.Value));
        if (!showAllTags)
            q = q.Where(r => IsPublicTag(r.Tag));
        return q.ToList();
    }
}

/// <summary>Compact chapter-metadata lines: reader merges adjacent <c>date</c>+<c>time</c>; debug prefixes each row with the tag.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Legacy callout display; reader path uses assembler datelines.")]
internal static class ChapterMetadataDisplay
{
    internal static readonly Regex HtmlTagStrip = new("<[^>]+>", RegexOptions.Compiled);

    public static List<string> BuildPlainLines(List<(string Tag, string Value)> rows, bool debugMode)
    {
        if (rows.Count == 0)
            return [];

        if (debugMode)
        {
            return rows.Where(r => !string.IsNullOrWhiteSpace(r.Value))
                .Select(r => $"{r.Tag.ToUpperInvariant()}  {r.Value}")
                .ToList();
        }

        var lines = new List<string>();
        var i = 0;
        while (i < rows.Count)
        {
            var (tag, val) = rows[i];
            if (string.IsNullOrWhiteSpace(val))
            {
                i++;
                continue;
            }

            var tl = tag.ToLowerInvariant();
            if (tl == "date" && i + 1 < rows.Count
                && rows[i + 1].Tag.Equals("time", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(rows[i + 1].Value))
            {
                lines.Add($"{val} {rows[i + 1].Value}");
                i += 2;
            }
            else if (tl == "time" && i + 1 < rows.Count
                     && rows[i + 1].Tag.Equals("date", StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrWhiteSpace(rows[i + 1].Value))
            {
                lines.Add($"{rows[i + 1].Value} {val}");
                i += 2;
            }
            else
            {
                lines.Add(val);
                i++;
            }
        }

        return lines;
    }

    static bool LooksBlankValueHtml(string valueHtml) =>
        string.IsNullOrWhiteSpace(HtmlTagStrip.Replace(valueHtml, " ").Trim());

    /// <summary>Reader HTML: one line per place anchor; first line merges <c>date</c> and following <c>time</c> (or reverse).</summary>
    public static List<string> BuildReaderValueHtmlLines(List<(string TagKey, string ValueHtml)> rows)
    {
        var lines = new List<string>();
        var i = 0;
        while (i < rows.Count)
        {
            var (tagKey, valHtml) = rows[i];
            if (LooksBlankValueHtml(valHtml))
            {
                i++;
                continue;
            }

            var tl = tagKey.ToLowerInvariant();
            if (tl == "date" && i + 1 < rows.Count && rows[i + 1].TagKey.Equals("time", StringComparison.OrdinalIgnoreCase))
            {
                var tVal = rows[i + 1].ValueHtml;
                if (!LooksBlankValueHtml(tVal))
                {
                    lines.Add(valHtml + " " + tVal);
                    i += 2;
                    continue;
                }
            }
            else if (tl == "time" && i + 1 < rows.Count && rows[i + 1].TagKey.Equals("date", StringComparison.OrdinalIgnoreCase))
            {
                var dVal = rows[i + 1].ValueHtml;
                if (!LooksBlankValueHtml(dVal))
                {
                    lines.Add(dVal + " " + valHtml);
                    i += 2;
                    continue;
                }
            }

            lines.Add(valHtml);
            i++;
        }

        return lines;
    }
}
