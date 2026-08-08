using System.Text;

namespace Novolis.Manuscript;

/// <summary>Builds <see cref="BookPrintDocument"/> and assembles reader/author Markdown.</summary>
public static class BookPrintAssembler
{
    /// <summary>Builds a print view from chapter markdown source.</summary>
    public static ChapterPrintView FromChapterMarkdown(
        string markdown,
        string? id = null,
        string? sourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        var (meta, remainder, format) = ManuscriptMetadata.Parse(markdown);
        var (headingMarkdown, title, body) = SplitHeadingAndBody(remainder, meta);
        var resolvedId = string.IsNullOrWhiteSpace(id)
            ? (meta.Number ?? title)
            : id!;
        if (string.IsNullOrWhiteSpace(resolvedId))
            resolvedId = "chapter";

        var publicFields = BuildPublicFields(meta);
        var readerLines = MergeDateTimeLines(publicFields);
        var hidden = BuildHiddenFields(meta);
        return new ChapterPrintView(
            resolvedId.Trim(),
            title,
            headingMarkdown,
            publicFields,
            readerLines,
            hidden,
            body,
            sourcePath,
            format);
    }

    /// <summary>Loads chapter files into print views (skips missing files).</summary>
    public static IReadOnlyList<ChapterPrintView> FromChapterFiles(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var list = new List<ChapterPrintView>();
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                continue;
            var text = File.ReadAllText(path);
            if (text.StartsWith('\uFEFF'))
                text = text[1..];
            var stem = Path.GetFileNameWithoutExtension(path);
            list.Add(FromChapterMarkdown(text, stem, Path.GetFullPath(path)));
        }

        return list;
    }

    /// <summary>Builds a book print document from catalog <see cref="BookInfo"/>.</summary>
    public static BookPrintDocument FromBook(
        BookInfo book,
        string? seriesTitle = null,
        string? rights = null)
    {
        ArgumentNullException.ThrowIfNull(book);
        var chapters = FromChapterFiles(book.Chapters.Select(c => c.FilePath));
        return new BookPrintDocument(
            book.Id,
            new BookPrintCover(book.Title, book.Subtitle, seriesTitle ?? book.SeriesId, book.Author, rights),
            chapters,
            book.DebugMode);
    }

    /// <summary>
    /// Assembles Markdown for export.
    /// Reader mode: H1 + public dateline + body. Author/debug: tagged callouts for all fields.
    /// </summary>
    public static string AssembleMarkdown(
        BookPrintDocument document,
        bool authorMode = false)
    {
        ArgumentNullException.ThrowIfNull(document);
        var sb = new StringBuilder();
        for (var i = 0; i < document.Chapters.Count; i++)
        {
            if (i > 0)
                sb.AppendLine();
            AppendChapter(sb, document.Chapters[i], authorMode || document.DebugMode);
        }

        return sb.ToString();
    }

    /// <summary>Assembles reader Markdown from chapter file paths.</summary>
    public static string AssembleReaderMarkdownFromFiles(
        IEnumerable<string> chapterPaths,
        bool authorMode = false)
    {
        var views = FromChapterFiles(chapterPaths);
        var doc = new BookPrintDocument(
            "book",
            new BookPrintCover("book", null, null, null, null),
            views,
            DebugMode: authorMode);
        return AssembleMarkdown(doc, authorMode);
    }

    static void AppendChapter(StringBuilder sb, ChapterPrintView chapter, bool includeHidden)
    {
        if (!string.IsNullOrWhiteSpace(chapter.HeadingMarkdown))
            sb.AppendLine(chapter.HeadingMarkdown.TrimEnd());
        else if (!string.IsNullOrWhiteSpace(chapter.Title))
            sb.AppendLine("# " + chapter.Title.Trim());

        // Always emit public fields as [!tag] callouts so Markdig keeps a QuoteBlock that
        // PDF/HTML/TXT can recognize as chapter-metadata. Plain consecutive lines (former
        // reader path) collapse to one paragraph; soft line breaks become spaces in PDF.
        foreach (var (tag, value) in chapter.PublicFields)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.AppendLine($"> [!{tag}] {value}");
        }

        if (includeHidden)
        {
            foreach (var (key, value) in chapter.HiddenFields.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(value))
                    sb.AppendLine($"> [!{key}] {value}");
            }
        }

        var hasMeta = chapter.PublicFields.Count > 0
                      || (includeHidden && chapter.HiddenFields.Count > 0);
        if (hasMeta)
            sb.AppendLine();

        var body = chapter.BodyMarkdown.TrimEnd();
        if (body.Length > 0)
        {
            sb.Append(body);
            if (!body.EndsWith('\n'))
                sb.AppendLine();
        }
    }

    static (string HeadingMarkdown, string Title, string Body) SplitHeadingAndBody(
        string remainder,
        ManuscriptChapterMetadata meta)
    {
        var normalized = remainder.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        var i = 0;
        while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
            i++;

        string headingMarkdown = "";
        string title = meta.Title?.Trim() ?? "";
        if (i < lines.Length && IsAtxHeading(lines[i], out var level, out var headingText) && level == 1)
        {
            headingMarkdown = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(title))
                title = headingText;
            i++;
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
                i++;
            while (i < lines.Length && ManuscriptMetadata.IsCalloutLine(lines[i]))
                i++;
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
                i++;
        }
        else if (!string.IsNullOrWhiteSpace(title))
        {
            headingMarkdown = string.IsNullOrWhiteSpace(meta.Number)
                ? "# " + title
                : $"# Chapter {meta.Number} - {title}";
        }

        if (string.IsNullOrWhiteSpace(headingMarkdown) && string.IsNullOrWhiteSpace(title))
        {
            // No H1 in source — leave heading empty; body is remaining prose.
        }

        var body = i >= lines.Length ? "" : string.Join('\n', lines.Skip(i));
        return (headingMarkdown, title, body);
    }

    static bool IsAtxHeading(string line, out int level, out string text)
    {
        level = 0;
        text = "";
        var t = line.TrimEnd('\r');
        if (t.Length == 0 || t[0] != '#')
            return false;
        var n = 0;
        while (n < t.Length && t[n] == '#')
            n++;
        if (n == 0 || n > 6)
            return false;
        if (n < t.Length && !char.IsWhiteSpace(t[n]))
            return false;
        level = n;
        text = t[n..].Trim();
        return text.Length > 0;
    }

    static List<(string Tag, string Value)> BuildPublicFields(ManuscriptChapterMetadata meta)
    {
        var rows = new List<(string Tag, string Value)>();
        void Add(string tag, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                rows.Add((tag, value.Trim()));
        }

        Add("date", meta.Date);
        Add("time", meta.Time);
        Add("system", meta.System);
        Add("location", meta.Location);
        return rows;
    }

    static Dictionary<string, string> BuildHiddenFields(ManuscriptChapterMetadata meta)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                map[key] = value.Trim();
        }

        Add("pov", meta.Pov);
        Add("characters", meta.Characters);
        Add("status", meta.Status);
        Add("notes", meta.Notes);
        foreach (var kv in meta.Extra)
        {
            if (ChapterMetadataVisibility.IsPublicTag(kv.Key))
                continue;
            Add(kv.Key, kv.Value);
        }

        return map;
    }

    /// <summary>Merges adjacent date+time into one line; other public tags stay as value-only lines.</summary>
    public static List<string> MergeDateTimeLines(IReadOnlyList<(string Tag, string Value)> rows)
    {
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
}
