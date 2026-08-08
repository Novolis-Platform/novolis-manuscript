using System.Globalization;
using System.Text.RegularExpressions;
using Novolis.Manuscript.Protocol;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Novolis.Manuscript.LegacyBooks;

/// <summary>
/// Reads legacy <c>content/series</c> and <c>content/books</c> trees into an NMP/1 <see cref="ManuscriptSnapshot"/>.
/// </summary>
public sealed class LegacyBooksCatalogReader
{
    const string SyntheticUniverseId = "legacy";
    const string SyntheticSubjectId = "legacy";

    static readonly IDeserializer Yaml =
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    static readonly Regex HeadingGeneric = new(@"^\s*#\s+(.+)\s*$", RegexOptions.Compiled);
    static readonly Regex HeadingChapter = new(
        @"^\s*#\s*Chapter\s+(\d+(?:\.\d+)?)\s*-\s*(.+)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex BooktoolsComment = new(
        @"<!--\s*booktools-chapter:\s*([\d.]+)\s*-->",
        RegexOptions.Compiled);
    static readonly Regex FrontMatter = new(
        @"(?s)^---\s*\r?\n(.*?)\r?\n---\s*\r?\n",
        RegexOptions.Compiled);
    static readonly Regex YamlChapter = new(
        @"^\s*chapter:\s*([\d.]+)\s*(?:#.*)?$",
        RegexOptions.Compiled | RegexOptions.Multiline);
    static readonly Regex CalloutLine = new(
        @"^>\s*\[!([A-Za-z0-9_-]+)\]\s*(.*)$",
        RegexOptions.Compiled);
    static readonly Regex NumericPrefix = new(
        @"^(?<order>\d+)-(?<slug>.+)$",
        RegexOptions.Compiled);

    /// <summary>Loads a legacy books repository root into a protocol snapshot.</summary>
    public ManuscriptSnapshot Read(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var contentRoot = Path.GetFullPath(root);
        if (!Directory.Exists(contentRoot))
            throw new DirectoryNotFoundException($"Content root not found: {contentRoot}");

        var diagnostics = new List<ManuscriptDiagnostic>();
        var seriesNodes = new List<ManuscriptSeries>();

        var seriesRoot = Path.Combine(contentRoot, "content", "series");
        if (Directory.Exists(seriesRoot))
        {
            foreach (var seriesDir in Directory.GetDirectories(seriesRoot).OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                var series = LoadSeries(seriesDir, diagnostics);
                seriesNodes.Add(series);
            }
        }

        var standalone = new List<ManuscriptBook>();
        var booksRoot = Path.Combine(contentRoot, "content", "books");
        if (Directory.Exists(booksRoot))
        {
            foreach (var bookDir in Directory.GetDirectories(booksRoot).OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                var book = LoadBook(
                    bookDir,
                    new ManuscriptAddress(ManuscriptKind.NonFiction, SyntheticSubjectId, null, Path.GetFileName(bookDir)),
                    diagnostics);
                standalone.Add(book);
            }
        }

        var fiction = new FictionUniverse(
            SyntheticUniverseId,
            new UniverseMetadata("Legacy", "Synthetic universe for legacy content/series trees."),
            seriesNodes,
            [],
            []);

        var nonFiction = new NonFictionSubject(
            SyntheticSubjectId,
            new SubjectMetadata("Legacy", "Synthetic subject for legacy content/books trees."),
            standalone,
            []);

        return new ManuscriptSnapshot(
            new ManuscriptCatalog([fiction], [nonFiction]),
            diagnostics);
    }

    ManuscriptSeries LoadSeries(string seriesDirectory, List<ManuscriptDiagnostic> diagnostics)
    {
        var yamlPath = Path.Combine(seriesDirectory, "series.yaml");
        var map = LoadYamlMap(yamlPath);
        var id = GetString(map, "id") ?? Path.GetFileName(seriesDirectory);
        var title = GetString(map, "name") ?? GetString(map, "title") ?? id;

        var books = new List<ManuscriptBook>();
        var booksDir = Path.Combine(seriesDirectory, "books");
        if (Directory.Exists(booksDir))
        {
            foreach (var bookDir in Directory.GetDirectories(booksDir).OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                books.Add(LoadBook(
                    bookDir,
                    new ManuscriptAddress(ManuscriptKind.Fiction, SyntheticUniverseId, id, Path.GetFileName(bookDir)),
                    diagnostics));
            }
        }

        var refs = LoadReferences(
            seriesDirectory,
            $"fiction/{SyntheticUniverseId}/{id}/reference",
            diagnostics);

        return new ManuscriptSeries(id, new SeriesMetadata(title), books, refs);
    }

    ManuscriptBook LoadBook(string bookDirectory, ManuscriptAddress address, List<ManuscriptDiagnostic> diagnostics)
    {
        var yamlPath = Path.Combine(bookDirectory, "book.yaml");
        var map = LoadYamlMap(yamlPath);
        var title = GetString(map, "title") ?? address.BookId;
        var subtitle = GetString(map, "subtitle");
        var author = GetString(map, "author");
        var authors = author is null ? null : (IReadOnlyList<string>)[author];
        var orderFromHeading = GetBool(map, "chapter_order_from_heading");
        var language = GetString(map, "language");
        var description = GetString(map, "description");
        var rights = GetString(map, "rights");

        var chapters = LoadLegacyDocuments(
            Path.Combine(bookDirectory, "chapters"),
            ManuscriptDocumentKind.Chapter,
            orderFromHeading,
            diagnostics);

        var appendices = LoadLegacyDocuments(
            Path.Combine(bookDirectory, "appendices"),
            ManuscriptDocumentKind.Appendix,
            orderFromHeading: false,
            diagnostics);

        var refs = LoadReferences(
            bookDirectory,
            address.SeriesId is null
                ? $"nonfiction/{address.ScopeId}/{address.BookId}/reference"
                : $"fiction/{address.ScopeId}/{address.SeriesId}/{address.BookId}/reference",
            diagnostics);

        var metadata = new BookMetadata(
            title,
            subtitle,
            Order: null,
            authors,
            language,
            description,
            rights);

        if (chapters.Count == 0)
        {
            diagnostics.Add(new ManuscriptDiagnostic(
                ManuscriptDiagnosticSeverity.Warning,
                ManuscriptDiagnosticCodes.EmptyBook,
                $"Book '{address.BookId}' has no chapters.",
                bookDirectory));
        }

        return new ManuscriptBook(address, metadata, chapters, appendices, refs);
    }

    List<ManuscriptDocument> LoadLegacyDocuments(
        string directory,
        ManuscriptDocumentKind kind,
        bool orderFromHeading,
        List<ManuscriptDiagnostic> diagnostics)
    {
        if (!Directory.Exists(directory))
            return [];

        var items = new List<(ManuscriptDocument Doc, double SortKey)>();
        var index = 0;
        foreach (var file in Directory.GetFiles(directory, "*.md").OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            var stem = Path.GetFileNameWithoutExtension(file)!;
            var title = ReadTitle(file) ?? stem;
            var metadata = ReadLegacyChapterMetadata(file);
            var sortKey = orderFromHeading ? GetSortKey(file) : index;
            var order = ResolveOrder(stem, sortKey, index);
            var slug = ResolveSlug(stem);

            items.Add((new ManuscriptDocument(slug, order, title, kind, file, metadata), sortKey));
            index++;
        }

        return items
            .OrderBy(i => i.SortKey)
            .ThenBy(i => i.Doc.FilePath, StringComparer.OrdinalIgnoreCase)
            .Select((i, n) => i.Doc with { Order = orderFromHeading ? ResolveOrder(i.Doc.Slug, i.SortKey, n) : n + 1 })
            .ToList();
    }

    static int ResolveOrder(string stem, double sortKey, int fallbackIndex)
    {
        var m = NumericPrefix.Match(stem);
        if (m.Success && int.TryParse(m.Groups["order"].Value, out var n) && n > 0)
            return n;

        if (!double.IsInfinity(sortKey) && sortKey > 0 && Math.Abs(sortKey - Math.Truncate(sortKey)) < double.Epsilon)
            return (int)sortKey;

        return fallbackIndex + 1;
    }

    static string ResolveSlug(string stem)
    {
        var m = NumericPrefix.Match(stem);
        return m.Success ? m.Groups["slug"].Value : stem;
    }

    List<ReferenceDocument> LoadReferences(string containerDir, string scopePrefix, List<ManuscriptDiagnostic> diagnostics)
    {
        var refRoot = ResolveReferenceRoot(containerDir);
        if (refRoot is null)
            return [];

        var results = new List<ReferenceDocument>();
        foreach (var path in Directory.GetFiles(refRoot, "*.md", SearchOption.AllDirectories)
                     .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Any(s => s.StartsWith('_') || s.StartsWith('.')))
                continue;

            var relative = Path.GetRelativePath(refRoot, path).Replace('\\', '/');
            if (relative.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                relative = relative[..^3];

            var title = ReadTitle(path) ?? Path.GetFileNameWithoutExtension(path)!;
            results.Add(new ReferenceDocument(
                $"{scopePrefix}/{relative}",
                title,
                relative,
                path,
                new ReferenceMetadata()));
        }

        if (results.Count == 0)
        {
            diagnostics.Add(new ManuscriptDiagnostic(
                ManuscriptDiagnosticSeverity.Warning,
                ManuscriptDiagnosticCodes.EmptyReferenceFolder,
                "References folder is empty.",
                refRoot));
        }

        return results;
    }

    static string? ResolveReferenceRoot(string containerDir)
    {
        var references = Path.Combine(containerDir, "references");
        if (Directory.Exists(references))
            return references;
        var reference = Path.Combine(containerDir, "reference");
        return Directory.Exists(reference) ? reference : null;
    }

    static ChapterMetadata ReadLegacyChapterMetadata(string filePath)
    {
        var text = File.ReadAllText(filePath);
        if (text.StartsWith('\uFEFF'))
            text = text[1..];

        var fm = FrontMatter.Match(text);
        if (fm.Success)
        {
            var map = LoadYamlMapFromText(fm.Groups[1].Value);
            return new ChapterMetadata(
                Status: GetString(map, "status"),
                Tags: GetStringList(map, "tags"),
                Date: GetString(map, "date"),
                Time: GetString(map, "time"),
                System: GetString(map, "system"),
                Locations: GetStringList(map, "locations") ?? SingleList(GetString(map, "location")),
                Pov: GetString(map, "pov"),
                Characters: GetStringList(map, "characters") ?? SingleList(GetString(map, "characters")));
        }

        // Callout fallback
        string? date = null, time = null, system = null, location = null, pov = null, characters = null, status = null;
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            var m = CalloutLine.Match(line.TrimEnd('\r'));
            if (!m.Success)
                continue;
            var key = m.Groups[1].Value.ToLowerInvariant();
            var value = m.Groups[2].Value.Trim();
            switch (key)
            {
                case "date": date = value; break;
                case "time": time = value; break;
                case "system": system = value; break;
                case "location":
                case "loc": location = value; break;
                case "pov":
                case "point_of_view": pov = value; break;
                case "characters":
                case "chars": characters = value; break;
                case "status": status = value; break;
            }
        }

        return new ChapterMetadata(
            status,
            null,
            date,
            time,
            system,
            SingleList(location),
            pov,
            SingleList(characters));
    }

    static double GetSortKey(string filePath)
    {
        var raw = File.ReadAllText(filePath);
        if (raw.StartsWith('\uFEFF'))
            raw = raw[1..];

        var lines = raw.Split(["\r\n", "\n"], StringSplitOptions.None);
        for (var i = 0; i < Math.Min(20, lines.Length); i++)
        {
            var cm = BooktoolsComment.Match(lines[i]);
            if (cm.Success)
                return double.Parse(cm.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        var fm = FrontMatter.Match(raw);
        if (fm.Success)
        {
            var ym = YamlChapter.Match(fm.Groups[1].Value);
            if (ym.Success)
                return double.Parse(ym.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        foreach (var line in lines)
        {
            var hm = HeadingChapter.Match(line);
            if (hm.Success)
                return double.Parse(hm.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        var stem = Path.GetFileNameWithoutExtension(filePath)!;
        var prefix = NumericPrefix.Match(stem);
        if (prefix.Success && double.TryParse(prefix.Groups["order"].Value, CultureInfo.InvariantCulture, out var n))
            return n;

        return double.PositiveInfinity;
    }

    static string? ReadTitle(string filePath)
    {
        foreach (var line in File.ReadLines(filePath))
        {
            var chapter = HeadingChapter.Match(line);
            if (chapter.Success)
                return chapter.Groups[2].Value.Trim();
            var generic = HeadingGeneric.Match(line);
            if (generic.Success)
                return generic.Groups[1].Value.Trim();
        }

        return null;
    }

    static Dictionary<string, object?> LoadYamlMap(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        return LoadYamlMapFromText(File.ReadAllText(path));
    }

    static Dictionary<string, object?> LoadYamlMapFromText(string raw)
    {
        try
        {
            var obj = Yaml.Deserialize<Dictionary<object, object?>>(raw);
            if (obj is null)
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in obj)
                result[key?.ToString() ?? string.Empty] = value;
            return result;
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    static string? GetString(Dictionary<string, object?> dict, string key) =>
        dict.TryGetValue(key, out var v) && v is not null ? v.ToString()?.Trim() : null;

    static bool GetBool(Dictionary<string, object?> dict, string key, bool defaultValue = false)
    {
        if (!dict.TryGetValue(key, out var v) || v is null)
            return defaultValue;
        if (v is bool b)
            return b;
        return bool.TryParse(v.ToString(), out var parsed) && parsed;
    }

    static IReadOnlyList<string>? GetStringList(Dictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var v) || v is null)
            return null;
        if (v is IList<object> list)
            return list.Select(x => x?.ToString()?.Trim()).Where(x => !string.IsNullOrEmpty(x)).Cast<string>().ToList();
        var s = v.ToString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : [s];
    }

    static IReadOnlyList<string>? SingleList(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : [value.Trim()];
}
