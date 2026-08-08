using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace Novolis.Manuscript;

/// <summary>Known metadata format for a chapter document.</summary>
public enum ManuscriptMetadataFormat
{
    /// <summary>No recognized metadata block.</summary>
    None,
    /// <summary>Obsidian-style <c>&gt; [!tag]</c> callouts.</summary>
    Callout,
    /// <summary>YAML front matter between <c>---</c> fences.</summary>
    Yaml
}

/// <summary>Parsed chapter metadata fields.</summary>
public sealed class ManuscriptChapterMetadata
{
    /// <summary>Chapter number string.</summary>
    public string? Number { get; set; }

    /// <summary>Chapter title.</summary>
    public string? Title { get; set; }

    /// <summary>Date field.</summary>
    public string? Date { get; set; }

    /// <summary>Time field.</summary>
    public string? Time { get; set; }

    /// <summary>System / location volume.</summary>
    public string? System { get; set; }

    /// <summary>Location.</summary>
    public string? Location { get; set; }

    /// <summary>Point of view.</summary>
    public string? Pov { get; set; }

    /// <summary>Characters list.</summary>
    public string? Characters { get; set; }

    /// <summary>Status.</summary>
    public string? Status { get; set; }

    /// <summary>Notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Additional unknown callout keys.</summary>
    public Dictionary<string, string> Extra { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Parse and apply chapter metadata callouts / YAML.</summary>
public static class ManuscriptMetadata
{
    static readonly Regex YamlFrontMatterRegex = new(
        @"^---\r?\n(.*?)\r?\n---\r?\n?",
        RegexOptions.Singleline | RegexOptions.Compiled);

    static readonly Regex CalloutLineRegex = new(
        @"^>\s*\[!([A-Za-z0-9_-]+)\]\s*(.*)$",
        RegexOptions.Compiled);

    static readonly Regex ChapterHeadingRegex = new(
        @"^#\s*Chapter\s+(\d+(?:\.\d+)?)\s*-\s*(.+?)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex AnyH1Regex = new(
        @"^#\s+(.+?)\s*$",
        RegexOptions.Compiled);

    /// <summary>Whether a line is an Obsidian-style metadata callout.</summary>
    public static bool IsCalloutLine(string line) =>
        CalloutLineRegex.IsMatch(line.TrimEnd('\r'));

    /// <summary>Parses metadata and returns body text after the preamble (YAML fences stripped).</summary>
    public static (ManuscriptChapterMetadata Meta, string Body, ManuscriptMetadataFormat Format) Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var yaml = YamlFrontMatterRegex.Match(text);
        if (yaml.Success)
        {
            var meta = new ManuscriptChapterMetadata();
            ParseYamlBlock(yaml.Groups[1].Value, meta);
            var afterYaml = text[yaml.Length..];
            ParseHeadingInto(afterYaml, meta);
            // Strip trailing callouts after H1 if a migrated hybrid exists.
            var (calloutEnd, hasCallouts) = ParseCalloutBlock(afterYaml, meta);
            var body = hasCallouts ? afterYaml[FindBodyStart(afterYaml, calloutEnd)..] : afterYaml;
            return (meta, body, ManuscriptMetadataFormat.Yaml);
        }

        var metaCallout = new ManuscriptChapterMetadata();
        ParseHeadingInto(text, metaCallout);
        var (end, has) = ParseCalloutBlock(text, metaCallout);
        if (has)
        {
            var withHeading = KeepHeadingStripCallouts(text, end);
            var body = withHeading ?? text[FindBodyStart(text, end)..];
            return (metaCallout, body, ManuscriptMetadataFormat.Callout);
        }

        if (!string.IsNullOrEmpty(metaCallout.Number))
            return (metaCallout, text, ManuscriptMetadataFormat.Callout);

        return (metaCallout, text, ManuscriptMetadataFormat.None);
    }

    /// <summary>Returns body suitable for word counting (strips heading and metadata).</summary>
    public static string GetBodyForWordCount(string text)
    {
        var (meta, remainder, format) = Parse(text);
        _ = meta;
        var lines = remainder.Replace("\r\n", "\n").Split('\n');
        var i = 0;
        while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
            i++;
        if (i < lines.Length && AnyH1Regex.IsMatch(lines[i].TrimEnd('\r')))
        {
            i++;
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
                i++;
            while (i < lines.Length && CalloutLineRegex.IsMatch(lines[i].TrimEnd('\r')))
                i++;
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
                i++;
            return string.Join('\n', lines.Skip(i));
        }

        // Callout format may already have stripped callouts but left prose only.
        if (format == ManuscriptMetadataFormat.Callout)
            return remainder.TrimStart('\r', '\n');

        return remainder;
    }

    /// <summary>Counts whitespace-separated words in the chapter body.</summary>
    public static int CountWords(string text)
    {
        var body = GetBodyForWordCount(text);
        if (string.IsNullOrWhiteSpace(body))
            return 0;
        return Regex.Matches(body, @"\S+").Count;
    }

    /// <summary>Applies metadata as callout lines after the H1.</summary>
    public static string ApplyCallouts(string text, ManuscriptChapterMetadata meta)
    {
        ArgumentNullException.ThrowIfNull(meta);
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
        var i = 0;
        while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i]))
            i++;

        if (i < lines.Count && ChapterHeadingRegex.IsMatch(lines[i].TrimEnd('\r')))
        {
            if (!string.IsNullOrWhiteSpace(meta.Number) && !string.IsNullOrWhiteSpace(meta.Title))
                lines[i] = $"# Chapter {meta.Number} - {meta.Title}";
            i++;
        }
        else if (i < lines.Count && AnyH1Regex.IsMatch(lines[i].TrimEnd('\r')))
        {
            if (!string.IsNullOrWhiteSpace(meta.Title))
            {
                lines[i] = string.IsNullOrWhiteSpace(meta.Number)
                    ? "# " + meta.Title.Trim()
                    : $"# Chapter {meta.Number} - {meta.Title.Trim()}";
            }

            i++;
        }
        else if (!string.IsNullOrWhiteSpace(meta.Number) && !string.IsNullOrWhiteSpace(meta.Title))
        {
            lines.Insert(i, $"# Chapter {meta.Number} - {meta.Title}");
            i++;
        }
        else if (!string.IsNullOrWhiteSpace(meta.Title))
        {
            lines.Insert(i, "# " + meta.Title.Trim());
            i++;
        }

        while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i]))
            i++;
        while (i < lines.Count && CalloutLineRegex.IsMatch(lines[i].TrimEnd('\r')))
            lines.RemoveAt(i);

        var callouts = new List<string>();
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                callouts.Add($"> [!{key}] {value.Trim()}");
        }

        Add("date", meta.Date);
        Add("time", meta.Time);
        Add("system", meta.System);
        Add("location", meta.Location);
        Add("pov", meta.Pov);
        Add("characters", meta.Characters);
        Add("status", meta.Status);
        Add("notes", meta.Notes);
        foreach (var kv in meta.Extra)
            Add(kv.Key, kv.Value);

        if (callouts.Count > 0)
        {
            lines.Insert(i, "");
            lines.InsertRange(i + 1, callouts);
            lines.Insert(i + 1 + callouts.Count, "");
        }

        return string.Join('\n', lines);
    }

    [ExcludeFromCodeCoverage(Justification = "Heading retention helper for callout strip.")]
    static string? KeepHeadingStripCallouts(string text, int calloutEnd)
    {
        var normalized = text.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        var i = 0;
        while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
            i++;
        if (i >= lines.Length || !AnyH1Regex.IsMatch(lines[i].TrimEnd('\r')))
            return null;

        var heading = lines[i];
        var bodyStart = FindBodyStart(text, calloutEnd);
        var body = text[bodyStart..];
        if (string.IsNullOrWhiteSpace(body))
            return heading + "\n";
        return heading + "\n\n" + body.TrimStart('\r', '\n');
    }

    static void ParseHeadingInto(string text, ManuscriptChapterMetadata meta)
    {
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var trimmed = line.TrimEnd('\r');
            var chapter = ChapterHeadingRegex.Match(trimmed);
            if (chapter.Success)
            {
                meta.Number = chapter.Groups[1].Value;
                meta.Title = chapter.Groups[2].Value.Trim();
                break;
            }

            var h1 = AnyH1Regex.Match(trimmed);
            if (h1.Success)
            {
                meta.Title = h1.Groups[1].Value.Trim();
                break;
            }

            break;
        }
    }

    static (int EndIndex, bool HasCallouts) ParseCalloutBlock(string text, ManuscriptChapterMetadata meta)
    {
        var normalized = text.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        var i = 0;
        while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
            i++;
        if (i < lines.Length && AnyH1Regex.IsMatch(lines[i].TrimEnd('\r')))
            i++;
        while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
            i++;

        var has = false;
        var index = 0;
        for (var lineNo = 0; lineNo < lines.Length; lineNo++)
        {
            var line = lines[lineNo];
            var lineLen = line.Length + (lineNo < lines.Length - 1 ? 1 : 0);
            if (lineNo < i)
            {
                index += lineLen;
                continue;
            }

            var m = CalloutLineRegex.Match(line.TrimEnd('\r'));
            if (!m.Success)
                break;
            has = true;
            ApplyCalloutLine(meta, m.Groups[1].Value, m.Groups[2].Value);
            index += lineLen;
        }

        return (index, has);
    }

    static void ApplyCalloutLine(ManuscriptChapterMetadata meta, string firstTag, string remainder)
    {
        // Authors often put several [!tag] values on one callout line.
        var combined = $"[!{firstTag}] {remainder}";
        var parts = SplitInlineTags(combined);
        if (parts.Count == 0)
        {
            ApplyCallout(meta, firstTag, remainder);
            return;
        }

        foreach (var (tag, value) in parts)
            ApplyCallout(meta, tag, value);
    }

    [ExcludeFromCodeCoverage(Justification = "Inline multi-tag splitter edge cases.")]
    static List<(string Tag, string Value)> SplitInlineTags(string plain)
    {
        var list = new List<(string, string)>();
        plain = plain.Trim();
        if (plain.Length == 0)
            return list;
        var matches = Regex.Matches(plain, @"\[!([a-z0-9_-]+)\]\s*", RegexOptions.IgnoreCase);
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

    static int FindBodyStart(string text, int calloutEnd)
    {
        var normalized = text.Replace("\r\n", "\n");
        var i = calloutEnd;
        while (i < normalized.Length && (normalized[i] == '\n' || normalized[i] == '\r' || char.IsWhiteSpace(normalized[i])))
            i++;
        return i;
    }

    [ExcludeFromCodeCoverage(Justification = "Tag dispatch table; covered via Parse/ApplyCallouts public API.")]
    static void ApplyCallout(ManuscriptChapterMetadata meta, string key, string value)
    {
        value = value.Trim();
        switch (key.ToLowerInvariant())
        {
            case "date": meta.Date = value; break;
            case "time": meta.Time = value; break;
            case "system": meta.System = value; break;
            case "location":
            case "locations":
            case "loc": meta.Location = value; break;
            case "pov":
            case "point_of_view": meta.Pov = value; break;
            case "characters":
            case "chars": meta.Characters = value; break;
            case "status": meta.Status = value; break;
            case "notes":
            case "note": meta.Notes = value; break;
            case "tags":
                if (!string.IsNullOrWhiteSpace(value))
                    meta.Extra["tags"] = value;
                break;
            case "title":
                meta.Title = value;
                break;
            case "number":
            case "chapter":
                meta.Number = value;
                break;
            default: meta.Extra[key] = value; break;
        }
    }

    static void ParseYamlBlock(string yaml, ManuscriptChapterMetadata meta)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            return;

        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(yaml));
            if (stream.Documents.Count == 0)
                return;
            if (stream.Documents[0].RootNode is not YamlMappingNode map)
            {
                ParseYamlBlockNaive(yaml, meta);
                return;
            }

            foreach (var (keyNode, valueNode) in map.Children)
            {
                if (keyNode is not YamlScalarNode keyScalar || string.IsNullOrWhiteSpace(keyScalar.Value))
                    continue;
                var key = keyScalar.Value!;
                var coerced = CoerceYamlValue(valueNode);
                if (coerced is null)
                    continue;
                ApplyCallout(meta, key, coerced);
            }
        }
        catch
        {
            ParseYamlBlockNaive(yaml, meta);
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Naive YAML fallback when YamlDotNet rejects the block.")]
    static void ParseYamlBlockNaive(string yaml, ManuscriptChapterMetadata meta)
    {
        string? listKey = null;
        var listItems = new List<string>();
        void FlushList()
        {
            if (listKey is null || listItems.Count == 0)
            {
                listKey = null;
                listItems.Clear();
                return;
            }

            ApplyCallout(meta, listKey, string.Join(", ", listItems));
            listKey = null;
            listItems.Clear();
        }

        foreach (var raw in yaml.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.Length == 0 || line.TrimStart().StartsWith('#'))
            {
                FlushList();
                continue;
            }

            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("- ") && listKey is not null)
            {
                listItems.Add(trimmed[2..].Trim().Trim('"'));
                continue;
            }

            FlushList();
            var idx = trimmed.IndexOf(':');
            if (idx <= 0)
                continue;
            var key = trimmed[..idx].Trim();
            var value = trimmed[(idx + 1)..].Trim().Trim('"');
            if (value.Length == 0)
            {
                listKey = key;
                continue;
            }

            ApplyCallout(meta, key, value);
        }

        FlushList();
    }

    static string? CoerceYamlValue(YamlNode node) => node switch
    {
        YamlScalarNode s => NullIfEmpty(s.Value),
        YamlSequenceNode seq =>
            string.Join(", ", seq.Children.OfType<YamlScalarNode>()
                .Select(n => n.Value?.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))!),
        _ => NullIfEmpty(Convert.ToString(node, CultureInfo.InvariantCulture)),
    };

    static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
