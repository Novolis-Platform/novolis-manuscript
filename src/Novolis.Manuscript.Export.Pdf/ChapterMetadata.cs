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
