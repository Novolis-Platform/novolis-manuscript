using System.Text;
using System.Text.RegularExpressions;

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

    /// <summary>Parses metadata and returns body text after the preamble.</summary>
    public static (ManuscriptChapterMetadata Meta, string Body, ManuscriptMetadataFormat Format) Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var yaml = YamlFrontMatterRegex.Match(text);
        if (yaml.Success)
        {
            var meta = new ManuscriptChapterMetadata();
            ParseYamlBlock(yaml.Groups[1].Value, meta);
            ParseHeadingInto(text[yaml.Length..], meta);
            return (meta, text[yaml.Length..], ManuscriptMetadataFormat.Yaml);
        }

        var metaCallout = new ManuscriptChapterMetadata();
        ParseHeadingInto(text, metaCallout);
        var (calloutEnd, hasCallouts) = ParseCalloutBlock(text, metaCallout);
        if (hasCallouts || !string.IsNullOrEmpty(metaCallout.Number))
        {
            var bodyStart = FindBodyStart(text, calloutEnd);
            return (metaCallout, text[bodyStart..], ManuscriptMetadataFormat.Callout);
        }

        return (metaCallout, text, ManuscriptMetadataFormat.None);
    }

    /// <summary>Returns body suitable for word counting (strips heading/callouts when present).</summary>
    public static string GetBodyForWordCount(string text)
    {
        var (_, body, _) = Parse(text);
        var lines = body.Replace("\r\n", "\n").Split('\n');
        var i = 0;
        while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
            i++;
        if (i < lines.Length && ChapterHeadingRegex.IsMatch(lines[i].TrimEnd('\r')))
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

        return body;
    }

    /// <summary>Counts whitespace-separated words in the chapter body.</summary>
    public static int CountWords(string text)
    {
        var body = GetBodyForWordCount(text);
        if (string.IsNullOrWhiteSpace(body))
            return 0;
        return Regex.Matches(body, @"\S+").Count;
    }

    /// <summary>Applies metadata as callout lines after the H1 (preferred authoring format).</summary>
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
        else if (!string.IsNullOrWhiteSpace(meta.Number) && !string.IsNullOrWhiteSpace(meta.Title))
        {
            lines.Insert(i, $"# Chapter {meta.Number} - {meta.Title}");
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

    static void ParseHeadingInto(string text, ManuscriptChapterMetadata meta)
    {
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var m = ChapterHeadingRegex.Match(line.TrimEnd('\r'));
            if (m.Success)
            {
                meta.Number ??= m.Groups[1].Value;
                meta.Title ??= m.Groups[2].Value.Trim();
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
        if (i < lines.Length && ChapterHeadingRegex.IsMatch(lines[i].TrimEnd('\r')))
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
            ApplyCallout(meta, m.Groups[1].Value, m.Groups[2].Value);
            index += lineLen;
        }

        return (index, has);
    }

    static int FindBodyStart(string text, int calloutEnd)
    {
        var normalized = text.Replace("\r\n", "\n");
        var i = calloutEnd;
        while (i < normalized.Length && (normalized[i] == '\n' || normalized[i] == '\r' || char.IsWhiteSpace(normalized[i])))
            i++;
        return i;
    }

    static void ApplyCallout(ManuscriptChapterMetadata meta, string key, string value)
    {
        value = value.Trim();
        switch (key.ToLowerInvariant())
        {
            case "date": meta.Date = value; break;
            case "time": meta.Time = value; break;
            case "system": meta.System = value; break;
            case "location":
            case "loc": meta.Location = value; break;
            case "pov":
            case "point_of_view": meta.Pov = value; break;
            case "characters":
            case "chars": meta.Characters = value; break;
            case "status": meta.Status = value; break;
            case "notes":
            case "note": meta.Notes = value; break;
            default: meta.Extra[key] = value; break;
        }
    }

    static void ParseYamlBlock(string yaml, ManuscriptChapterMetadata meta)
    {
        foreach (var raw in yaml.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            var idx = line.IndexOf(':');
            if (idx <= 0)
                continue;
            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim().Trim('"');
            ApplyCallout(meta, key, value);
        }
    }
}
