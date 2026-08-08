using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Blockquotes where every non-empty paragraph is <c>[!tag] value</c> (chapter metadata).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Legacy callout quote parsing; reader path uses assembler datelines.")]
internal static class ChapterMetadataQuote
{
    static readonly Regex TagOpenings = new(@"\[!([a-z0-9_-]+)\]\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Extract every <c>[!tag] value</c> pair from plain text. Markdig often merges consecutive blockquote lines into one
    /// paragraph, so one paragraph may contain <c>[!date] ... [!time] ...</c>.
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

    public static bool TryGetRows(QuoteBlock q, out List<(string Tag, string Value)> rows)
    {
        rows = new List<(string, string)>();
        foreach (var inner in q)
        {
            if (inner is not ParagraphBlock pb)
                return false;
            var plain = PlainTextRenderer.InlinesToPlain(pb.Inline).Trim();
            if (plain.Length == 0)
                continue;
            var parts = SplitFieldsFromPlain(plain);
            if (parts.Count == 0)
            {
                rows.Clear();
                return false;
            }

            rows.AddRange(parts);
        }

        return rows.Count > 0;
    }
}

/// <summary>Which <c>[!tag]</c> lines appear in reader-facing builds.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Thin delegate to ChapterMetadataVisibility.")]
internal static class ChapterMetadataTagVisibility
{
    public static bool IsPublicTag(string tag) =>
        Novolis.Manuscript.ChapterMetadataVisibility.IsPublicTag(tag);

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

/// <summary>Rewrites chapter-metadata blockquotes in HTML with compact monospace lines for print CSS.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Legacy HTML polish; reader export correctness owned by BookPrintAssembler.")]
internal static class ChapterMetadataHtml
{
    public static string TransformBlockquotes(string html, bool showAllTags)
    {
        const string open = "<blockquote>";
        const string close = "</blockquote>";
        var sb = new StringBuilder(html.Length + 128);
        var pos = 0;
        while (pos < html.Length)
        {
            var i = html.IndexOf(open, pos, StringComparison.OrdinalIgnoreCase);
            if (i < 0)
            {
                sb.Append(html.AsSpan(pos));
                break;
            }

            sb.Append(html.AsSpan(pos, i - pos));
            var j = html.IndexOf(close, i + open.Length, StringComparison.OrdinalIgnoreCase);
            if (j < 0)
            {
                sb.Append(html.AsSpan(i));
                break;
            }

            var inner = html.Substring(i + open.Length, j - (i + open.Length));
            if (TryRewriteChapterMetadataInner(inner, showAllTags, out var rewritten))
            {
                if (rewritten.Length > 0)
                    sb.Append("<blockquote class=\"chapter-metadata\">").Append(rewritten).Append(close);
            }
            else
                sb.Append(open).Append(inner).Append(close);
            pos = j + close.Length;
        }

        return sb.ToString();
    }

    static bool TryRewriteChapterMetadataInner(string inner, bool showAllTags, out string result)
    {
        result = "";
        var parsed = new List<(string TagKey, string ValueHtml)>();
        var p = 0;
        while (p < inner.Length)
        {
            var ps = inner.IndexOf("<p>", p, StringComparison.OrdinalIgnoreCase);
            if (ps < 0)
                break;
            var pe = inner.IndexOf("</p>", ps, StringComparison.OrdinalIgnoreCase);
            if (pe < 0)
                return false;
            var content = inner.Substring(ps + 3, pe - (ps + 3));
            p = pe + 4;
            var stripped = ChapterMetadataDisplay.HtmlTagStrip.Replace(content, " ").Trim();
            var splits = ChapterMetadataQuote.SplitFieldsFromPlain(stripped);
            if (splits.Count == 0)
                return false;
            if (splits.Count == 1)
            {
                var closeBracket = content.IndexOf(']');
                if (closeBracket < 0)
                    return false;
                var valueHtml = content.Substring(closeBracket + 1).Trim();
                if (string.IsNullOrWhiteSpace(ChapterMetadataDisplay.HtmlTagStrip.Replace(valueHtml, " ").Trim()))
                    continue;
                parsed.Add((splits[0].Tag, valueHtml));
            }
            else
            {
                foreach (var (tag, val) in splits)
                {
                    if (string.IsNullOrWhiteSpace(val))
                        continue;
                    parsed.Add((tag, WebUtility.HtmlEncode(val)));
                }
            }
        }

        if (parsed.Count == 0)
            return false;

        var filtered = showAllTags
            ? parsed
            : parsed.Where(x => ChapterMetadataTagVisibility.IsPublicTag(x.TagKey)).ToList();
        if (filtered.Count == 0)
            return true;

        var sb = new StringBuilder();
        if (showAllTags)
        {
            foreach (var (tagKey, valueHtml) in filtered)
            {
                sb.Append("<p class=\"sl-debug-row\"><span class=\"sl-k\">")
                    .Append(WebUtility.HtmlEncode(tagKey.ToUpperInvariant()))
                    .Append("</span> <span class=\"sl-v\">").Append(valueHtml).Append("</span></p>\n");
            }
        }
        else
        {
            foreach (var lineHtml in ChapterMetadataDisplay.BuildReaderValueHtmlLines(filtered))
                sb.Append("<p class=\"sl-line\">").Append(lineHtml).Append("</p>\n");
        }

        result = sb.ToString();
        return true;
    }
}
