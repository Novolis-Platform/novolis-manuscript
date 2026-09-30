using System.Text.RegularExpressions;
using Novolis.Markup.Markdown;

namespace Novolis.Manuscript.Export.Pdf;

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
