using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Novolis.Manuscript;
using Novolis.Markup.Markdown;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>Kind of speech segment.</summary>
public enum SpeechSegmentKind
{
    /// <summary>Spoken text.</summary>
    Text,
    /// <summary>Silence / pause.</summary>
    Pause
}

/// <summary>One segment in a speech plan.</summary>
public sealed record SpeechSegment(SpeechSegmentKind Kind, string? Text, int PauseMs)
{
    /// <summary>Creates a spoken segment.</summary>
    public static SpeechSegment Spoken(string text) => new(SpeechSegmentKind.Text, text, 0);

    /// <summary>Creates a pause segment.</summary>
    public static SpeechSegment Pause(int milliseconds) => new(SpeechSegmentKind.Pause, null, milliseconds);
}

/// <summary>Voice / planner settings for manuscript speech.</summary>
public sealed class SpeechOptions
{
    /// <summary>Pause inserted between scene breaks (ms).</summary>
    public int SceneBreakMs { get; init; } = 1200;

    /// <summary>Maximum characters per spoken chunk.</summary>
    public int MaxChunkChars { get; init; } = 2800;

    /// <summary>Whole-word pronunciation rewrites (longest keys first).</summary>
    public IReadOnlyDictionary<string, string> Pronunciation { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Planned speech for a chapter.</summary>
public sealed class SpeechPlan
{
    /// <summary>Creates a plan.</summary>
    public SpeechPlan(IReadOnlyList<SpeechSegment> segments, string planHash)
    {
        Segments = segments;
        PlanHash = planHash;
    }

    /// <summary>Ordered segments.</summary>
    public IReadOnlyList<SpeechSegment> Segments { get; }

    /// <summary>Content-addressed hash of the plan.</summary>
    public string PlanHash { get; }
}

/// <summary>Builds TTS speech plans from manuscript markdown bodies.</summary>
public static class SpeechPlanner
{
    static readonly Regex SceneBreakRegex = new(@"^\s*(?:\*{3,}|_{3,}|-{3,})\s*$", RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>Creates a speech plan from markdown chapter text.</summary>
    public static SpeechPlan Create(string markdown, SpeechOptions? options = null, bool speakTitle = false)
    {
        options ??= new SpeechOptions();
        var normalized = Normalize(markdown, speakTitle);
        normalized = ApplyPronunciation(normalized, options.Pronunciation);

        var scenes = SceneBreakRegex.Split(normalized)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        var segments = new List<SpeechSegment>();
        for (var i = 0; i < scenes.Count; i++)
        {
            // Prefer blank-line paragraphs so listen can start after a short first synth.
            foreach (var paragraph in SplitParagraphs(scenes[i]))
            {
                foreach (var chunk in Chunk(paragraph, options.MaxChunkChars))
                    segments.Add(SpeechSegment.Spoken(chunk));
            }

            if (i < scenes.Count - 1 && options.SceneBreakMs > 0)
                segments.Add(SpeechSegment.Pause(options.SceneBreakMs));
        }

        var hash = HashPlan(segments, options);
        return new SpeechPlan(segments, hash);
    }

    /// <summary>Converts manuscript Markdown into readable speech paragraphs.</summary>
    public static string Normalize(string markdown, bool keepTitle)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        var view = BookPrintAssembler.FromChapterMarkdown(markdown);
        var blocks = new List<string>();
        var title = keepTitle && !string.IsNullOrWhiteSpace(view.Title)
            ? RenderInlineMarkdown(view.Title)
            : string.Empty;

        var body = string.Join(
            '\n',
            view.BodyMarkdown
                .Replace("\r\n", "\n")
                .Split('\n')
                .Where(static line =>
                {
                    var trimmed = line.Trim();
                    return !trimmed.StartsWith("> [!", StringComparison.Ordinal) &&
                           !trimmed.StartsWith(">[!", StringComparison.Ordinal);
                }));
        foreach (var section in MarkdownDocument.Parse(body))
        {
            switch (section)
            {
                case IMarkdownHeader header:
                    // Chapter titles are handled above; keep existing speech behavior
                    // of omitting Markdown headings from the spoken body.
                    break;
                case IMarkdownParagraph paragraph:
                    AddBlock(blocks, RenderParagraph(paragraph));
                    break;
                case IMarkdownUnorderedList unordered:
                    foreach (var item in unordered.Items)
                        AddBlock(blocks, RenderListItem(item, ordered: false));
                    break;
                case IMarkdownOrderedList ordered:
                    var number = 1;
                    foreach (var item in ordered.Items)
                        AddBlock(blocks, RenderListItem(item, ordered: true, number: number++));
                    break;
                case IMarkdownTable table:
                    AddTableBlocks(blocks, table);
                    break;
                case IMarkdownQuote quote:
                    AddBlock(
                        blocks,
                        "Quote. " + RenderInlineMarkdown(string.Join(' ', quote.Text)));
                    break;
                case IMarkdownCodeBlock code:
                    var language = string.IsNullOrWhiteSpace(code.Language)
                        ? string.Empty
                        : $" {RenderInlineMarkdown(code.Language)}";
                    AddBlock(
                        blocks,
                        $"Code block{language}. {CleanSpeechText(code.Code)}");
                    break;
                case IMarkdownHorizontalRule:
                    // Keep the existing scene-break pause behavior.
                    AddBlock(blocks, "***");
                    break;
                default:
                    AddBlock(blocks, RenderInlineMarkdown(section.ToString()));
                    break;
            }
        }

        if (title.Length > 0)
        {
            var firstSpoken = blocks.FindIndex(static block => block != "***");
            if (firstSpoken < 0)
                blocks.Insert(0, title);
            else
                blocks[firstSpoken] = title + "\n" + blocks[firstSpoken];
        }

        return string.Join("\n\n", blocks).Trim();
    }

    static void AddBlock(List<string> blocks, string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            blocks.Add(text.Trim());
    }

    static string RenderParagraph(IMarkdownParagraph paragraph)
    {
        var text = new StringBuilder();
        foreach (var item in paragraph.Items)
        {
            switch (item.Type)
            {
                case MarkdownParagraphItemType.Link:
                    // The preceding LinkText contains the human-readable label.
                    break;
                case MarkdownParagraphItemType.Indent:
                    text.Append(' ');
                    break;
                case MarkdownParagraphItemType.NewLine:
                    text.Append(' ');
                    break;
                default:
                    text.Append(item.Text);
                    break;
            }
        }

        return CleanSpeechText(text.ToString());
    }

    static string RenderInlineMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var document = MarkdownDocument.Parse(text);
        var rendered = string.Join(
            ' ',
            document
                .OfType<IMarkdownParagraph>()
                .Select(RenderParagraph)
                .Where(static value => value.Length > 0));
        return CleanSpeechText(string.IsNullOrWhiteSpace(rendered) ? text : rendered);
    }

    static string RenderListItem(string item, bool ordered, int number = 0)
    {
        var depth = MarkdownDocument.DecodeNestDepth(item, out var body);
        var prefix = ordered
            ? $"List item {number}"
            : "List item";
        if (depth > 0)
            prefix = $"Nested {prefix.ToLowerInvariant()}";
        return $"{prefix}. {RenderInlineMarkdown(body)}";
    }

    static void AddTableBlocks(List<string> blocks, IMarkdownTable table)
    {
        var headers = table.Headers.Select(RenderInlineMarkdown).ToArray();
        if (headers.Length > 0)
            AddBlock(blocks, "Table. " + string.Join(", ", headers));

        foreach (var row in table.Rows)
        {
            var cells = row.Select(RenderInlineMarkdown).ToArray();
            if (cells.Length == 0)
                continue;

            var values = cells
                .Select((cell, index) =>
                    index < headers.Length && headers[index].Length > 0
                        ? $"{headers[index]}: {cell}"
                        : cell)
                .Where(static value => value.Length > 0);
            AddBlock(blocks, "Table row. " + string.Join(". ", values));
        }
    }

    static string CleanSpeechText(string text)
    {
        var cleaned = Regex.Replace(text, @"!\[([^\]]*)\]\([^)]*\)", "$1");
        cleaned = Regex.Replace(cleaned, @"<[^>]+>", string.Empty);
        cleaned = cleaned.Replace("\\", string.Empty, StringComparison.Ordinal);
        cleaned = Regex.Replace(cleaned, @"[*_~`]+", string.Empty);
        return Regex.Replace(cleaned, @"\s+", " ").Trim();
    }

    /// <summary>Applies whole-word pronunciation rewrites (longest keys first).</summary>
    public static string ApplyPronunciation(string text, IReadOnlyDictionary<string, string> map)
    {
        if (map.Count == 0)
            return text;
        var ordered = map.OrderByDescending(kv => kv.Key.Length).ToList();
        foreach (var (key, value) in ordered)
        {
            var pattern = $@"\b{Regex.Escape(key)}\b";
            text = Regex.Replace(text, pattern, value, RegexOptions.IgnoreCase);
        }

        return text;
    }

    /// <summary>Splits scene text on blank lines into paragraphs.</summary>
    public static IReadOnlyList<string> SplitParagraphs(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var normalized = text.Replace("\r\n", "\n").Trim();
        if (normalized.Length == 0)
            return [];

        return normalized
            .Split("\n\n", StringSplitOptions.None)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();
    }

    /// <summary>Splits text into chunks not exceeding <paramref name="maxChars"/>.</summary>
    public static IReadOnlyList<string> Chunk(string text, int maxChars)
    {
        if (maxChars < 32)
            throw new ArgumentOutOfRangeException(nameof(maxChars));
        text = text.Trim();
        if (text.Length == 0)
            return [];
        if (text.Length <= maxChars)
            return [text];

        var chunks = new List<string>();
        var remaining = text;
        while (remaining.Length > maxChars)
        {
            var window = remaining[..maxChars];
            var splitAt = window.LastIndexOfAny(['.', '!', '?', '\n']);
            if (splitAt < maxChars / 3)
                splitAt = window.LastIndexOf(' ');
            if (splitAt < maxChars / 4)
                splitAt = maxChars;
            else
                splitAt += 1;

            chunks.Add(remaining[..splitAt].Trim());
            remaining = remaining[splitAt..].TrimStart();
        }

        if (remaining.Length > 0)
            chunks.Add(remaining);
        return chunks;
    }

    static string HashPlan(IReadOnlyList<SpeechSegment> segments, SpeechOptions options)
    {
        var sb = new StringBuilder();
        sb.Append(options.SceneBreakMs).Append('|').Append(options.MaxChunkChars).Append('|');
        foreach (var kv in options.Pronunciation.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            sb.Append(kv.Key).Append('=').Append(kv.Value).Append(';');
        foreach (var seg in segments)
            sb.Append((int)seg.Kind).Append(':').Append(seg.Text).Append(':').Append(seg.PauseMs).Append('|');
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
