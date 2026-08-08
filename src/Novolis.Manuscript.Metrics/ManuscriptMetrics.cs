using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Metrics;

/// <summary>Per-chapter metrics row.</summary>
public sealed class ChapterMetricRow
{
    /// <summary>Chapter file name.</summary>
    public string File { get; init; } = "";

    /// <summary>Approximate word count.</summary>
    public int Words { get; init; }

    /// <summary>TODO/FIXME/TK needle count.</summary>
    public int Todos { get; init; }
}

/// <summary>Aggregated book metrics.</summary>
public sealed class BookMetricsDto
{
    /// <summary>Series id (or <c>books</c> for standalone).</summary>
    public string Series { get; init; } = "";

    /// <summary>Book id.</summary>
    public string Book { get; init; } = "";

    /// <summary>Book title from yaml when present.</summary>
    public string? Title { get; init; }

    /// <summary>Target words from book.yaml when present.</summary>
    public int? TargetWords { get; init; }

    /// <summary>Total words across chapters.</summary>
    public int TotalWords { get; init; }

    /// <summary>Total TODO/FIXME/TK counts.</summary>
    public int TotalTodos { get; init; }

    /// <summary>Estimated reading hours at ~9300 words/hour.</summary>
    public double EstimatedHours { get; init; }

    /// <summary>Per-chapter breakdown.</summary>
    public IReadOnlyList<ChapterMetricRow> Chapters { get; init; } = [];
}

/// <summary>Computes manuscript metrics (pure) and optional disk reporters.</summary>
public static class ManuscriptMetrics
{
    const double WordsPerHour = 9300.0;
    static readonly Regex FencedCode = new(@"```[\s\S]*?```", RegexOptions.Compiled | RegexOptions.Multiline);
    static readonly Regex ImageMd = new(@"!\[[^\]]*\]\([^\)]*\)", RegexOptions.Compiled);
    static readonly Regex LinkMd = new(@"\[([^\]]+)\]\([^\)]*\)", RegexOptions.Compiled);
    static readonly Regex MdNoise = new(@"[#>*_`~\[\]]+", RegexOptions.Compiled);
    static readonly Regex WordLike = new(@"\b[\p{L}\p{N}']+\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Computes metrics for every buildable book (no disk writes).</summary>
    public static IReadOnlyList<BookMetricsDto> ComputeAll(string workspaceRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        if (!ManuscriptWorkspace.TryOpen(workspaceRoot, out var ws) || ws is null)
            throw new InvalidOperationException($"Not a manuscript workspace: {workspaceRoot}");

        var results = new List<BookMetricsDto>();
        foreach (var series in ws.Catalog.Load(ws.ContentRoot))
        {
            foreach (var book in series.Books)
                results.Add(ComputeBook(series.Id, book));
        }

        foreach (var book in ws.Catalog.LoadStandaloneBooks(ws.ContentRoot))
            results.Add(ComputeBook("books", book));

        return results;
    }

    /// <summary>Computes metrics for one book (no disk writes).</summary>
    public static BookMetricsDto ComputeOne(string workspaceRoot, string seriesId, string bookId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(bookId);
        if (!ManuscriptWorkspace.TryOpen(workspaceRoot, out var ws) || ws is null)
            throw new InvalidOperationException($"Not a manuscript workspace: {workspaceRoot}");

        var book = ws.Catalog.FindBook(ws.ContentRoot, seriesId, bookId)
                   ?? throw new FileNotFoundException($"Book not found: {seriesId}/{bookId}");
        return ComputeBook(string.IsNullOrWhiteSpace(seriesId) ? "books" : seriesId, book);
    }

    /// <summary>Computes metrics for a loaded book (no disk writes).</summary>
    public static BookMetricsDto ComputeBook(string seriesId, BookInfo book)
    {
        ArgumentNullException.ThrowIfNull(book);
        var yaml = BookYaml.LoadFile(Path.Combine(book.DirectoryPath, "book.yaml"));
        var target = TryTargetWords(yaml);
        var chapters = new List<ChapterMetricRow>();
        var totalWords = 0;
        var totalTodos = 0;
        foreach (var chapter in book.Chapters.Where(c => c.Kind == ChapterKind.Chapter))
        {
            if (!File.Exists(chapter.FilePath))
                continue;
            var raw = File.ReadAllText(chapter.FilePath);
            var words = GetWordCount(raw);
            var todos = CountNeedles(raw, "TODO", "FIXME", "TK");
            totalWords += words;
            totalTodos += todos;
            chapters.Add(new ChapterMetricRow
            {
                File = Path.GetFileName(chapter.FilePath),
                Words = words,
                Todos = todos,
            });
        }

        return new BookMetricsDto
        {
            Series = seriesId,
            Book = book.Id,
            Title = book.Title,
            TargetWords = target,
            TotalWords = totalWords,
            TotalTodos = totalTodos,
            EstimatedHours = totalWords / WordsPerHour,
            Chapters = chapters,
        };
    }

    /// <summary>Computes all books and writes <c>out/</c> reports (CLI/CI path).</summary>
    public static IReadOnlyList<BookMetricsDto> RunAll(string workspaceRoot)
    {
        var results = ComputeAll(workspaceRoot);
        WriteAllReports(workspaceRoot, results);
        return results;
    }

    /// <summary>Computes one book and writes <c>out/</c> reports (CLI/CI path).</summary>
    public static BookMetricsDto RunOne(string workspaceRoot, string seriesId, string bookId)
    {
        var dto = ComputeOne(workspaceRoot, seriesId, bookId);
        WriteBookReports(workspaceRoot, dto);
        return dto;
    }

    /// <summary>Writes per-book JSON/MD under <c>out/</c> and an overview when multiple.</summary>
    public static void WriteAllReports(string workspaceRoot, IReadOnlyList<BookMetricsDto> results)
    {
        foreach (var dto in results)
            WriteBookReports(workspaceRoot, dto);
        WriteOverview(workspaceRoot, results);
    }

    /// <summary>Writes one book's metrics JSON/MD under <c>out/</c>.</summary>
    public static void WriteBookReports(string workspaceRoot, BookMetricsDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var outDir = ResolveOutMetricsDir(workspaceRoot, dto.Series, dto.Book);
        Directory.CreateDirectory(outDir);
        var jsonPath = Path.Combine(outDir, $"{dto.Book}.metrics.json");
        var mdPath = Path.Combine(outDir, $"{dto.Book}.metrics.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(dto, JsonOptions));
        File.WriteAllText(mdPath, FormatMarkdown(dto));
    }

    /// <summary>Counts approximate prose words in markdown.</summary>
    public static int GetWordCount(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return 0;
        var text = FencedCode.Replace(markdown, " ");
        text = ImageMd.Replace(text, " ");
        text = LinkMd.Replace(text, " $1 ");
        text = MdNoise.Replace(text, " ");
        return WordLike.Matches(text).Count;
    }

    /// <summary>Formats a book metrics DTO as Markdown.</summary>
    public static string FormatMarkdown(BookMetricsDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# Metrics — {dto.Title ?? dto.Book}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Series: `{dto.Series}`");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Book: `{dto.Book}`");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Words: {dto.TotalWords}");
        if (dto.TargetWords is int t)
            sb.AppendLine(CultureInfo.InvariantCulture, $"- Target words: {t}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- TODO/FIXME/TK: {dto.TotalTodos}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Estimated hours: {dto.EstimatedHours:0.00}");
        sb.AppendLine();
        sb.AppendLine("| Chapter | Words | Todos |");
        sb.AppendLine("|---|---:|---:|");
        foreach (var c in dto.Chapters)
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {c.File} | {c.Words} | {c.Todos} |");
        return sb.ToString();
    }

    static void WriteOverview(string workspaceRoot, IReadOnlyList<BookMetricsDto> results)
    {
        var dir = Path.Combine(workspaceRoot, "out", "metrics");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "overview.metrics.md");
        var sb = new StringBuilder();
        sb.AppendLine("# Metrics overview");
        sb.AppendLine();
        sb.AppendLine("| Series | Book | Words | Todos | Hours |");
        sb.AppendLine("|---|---|---:|---:|---:|");
        foreach (var r in results.OrderBy(x => x.Series, StringComparer.Ordinal).ThenBy(x => x.Book, StringComparer.Ordinal))
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"| {r.Series} | {r.Book} | {r.TotalWords} | {r.TotalTodos} | {r.EstimatedHours:0.00} |");
        }

        File.WriteAllText(path, sb.ToString());
    }

    static string ResolveOutMetricsDir(string workspaceRoot, string seriesId, string bookId) =>
        string.Equals(seriesId, "books", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(workspaceRoot, "out", bookId, "metrics")
            : Path.Combine(workspaceRoot, "out", seriesId, bookId, "metrics");

    static int? TryTargetWords(Dictionary<string, object?> yaml)
    {
        if (yaml.TryGetValue("targets", out var targets) && targets is not null)
        {
            foreach (var (k, v) in EnumerateMap(targets))
            {
                if (k.Equals("words", StringComparison.OrdinalIgnoreCase))
                    return CoerceInt(v);
            }
        }

        return CoerceInt(yaml.TryGetValue("target_words", out var tw) ? tw : null);
    }

    static IEnumerable<(string Key, object? Value)> EnumerateMap(object map)
    {
        if (map is Dictionary<object, object?> objMap)
        {
            foreach (var (k, v) in objMap)
                yield return (k?.ToString() ?? "", v);
            yield break;
        }

        if (map is System.Collections.IDictionary dict)
        {
            foreach (System.Collections.DictionaryEntry entry in dict)
                yield return (entry.Key?.ToString() ?? "", entry.Value);
        }
    }

    static int? CoerceInt(object? v) => v switch
    {
        null => null,
        int i => i,
        long l => (int)l,
        double d => (int)d,
        float f => (int)f,
        decimal m => (int)m,
        string s when double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDouble)
            => (int)parsedDouble,
        string s => int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null,
        _ => double.TryParse(v.ToString()?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var m)
            ? (int)m
            : null,
    };

    static int CountNeedles(string text, params string[] needles)
    {
        var count = 0;
        foreach (var needle in needles)
        {
            var idx = 0;
            while ((idx = text.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
            {
                count++;
                idx += needle.Length;
            }
        }

        return count;
    }
}
