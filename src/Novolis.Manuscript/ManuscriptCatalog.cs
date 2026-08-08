namespace Novolis.Manuscript;

/// <summary>Loads series/book/chapter catalogs from a content root (legacy <c>content/</c> or NMP/1 <c>src/</c>).</summary>
public sealed class ManuscriptCatalog
{
    /// <summary>Loads all fiction series (legacy <c>content/series</c> or NMP <c>src/Fiction/**</c>).</summary>
    public IReadOnlyList<SeriesInfo> Load(string contentRoot)
    {
        var root = Path.GetFullPath(contentRoot);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Content root not found: {root}");

        if (IsProtocolRoot(root))
            return LoadProtocolSeries(root);

        var seriesList = new List<SeriesInfo>();
        var seriesDir = Path.Combine(root, "content", "series");
        if (Directory.Exists(seriesDir))
        {
            foreach (var dir in Directory.GetDirectories(seriesDir).OrderBy(Path.GetFileName, StringComparer.Ordinal))
                seriesList.Add(LoadLegacySeries(dir));
        }

        return seriesList;
    }

    /// <summary>Loads standalone books (legacy <c>content/books</c> or NMP NonFiction books).</summary>
    public IReadOnlyList<BookInfo> LoadStandaloneBooks(string contentRoot)
    {
        var root = Path.GetFullPath(contentRoot);
        if (IsProtocolRoot(root))
            return LoadProtocolStandaloneBooks(root);

        var booksDir = Path.Combine(root, "content", "books");
        if (!Directory.Exists(booksDir))
            return [];

        return Directory.GetDirectories(booksDir)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .Select(dir => LoadBook(dir, seriesId: null, protocolLayout: false))
            .ToList();
    }

    /// <summary>Finds a book by series and/or book id.</summary>
    public BookInfo? FindBook(string contentRoot, string? seriesId, string bookId)
    {
        var catalog = Load(contentRoot);
        if (!string.IsNullOrWhiteSpace(seriesId))
        {
            var series = catalog.FirstOrDefault(s => s.Id.Equals(seriesId, StringComparison.OrdinalIgnoreCase));
            return series?.Books.FirstOrDefault(b => b.Id.Equals(bookId, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var s in catalog)
        {
            var book = s.Books.FirstOrDefault(b => b.Id.Equals(bookId, StringComparison.OrdinalIgnoreCase));
            if (book is not null)
                return book;
        }

        foreach (var book in LoadStandaloneBooks(contentRoot))
        {
            if (book.Id.Equals(bookId, StringComparison.OrdinalIgnoreCase))
                return book;
        }

        return null;
    }

    static bool IsProtocolRoot(string root) =>
        File.Exists(Path.Combine(root, "manuscript.yaml"))
        || Directory.Exists(Path.Combine(root, "src", "Fiction"))
        || Directory.Exists(Path.Combine(root, "src", "NonFiction"));

    static List<SeriesInfo> LoadProtocolSeries(string root)
    {
        var seriesList = new List<SeriesInfo>();
        var fictionRoot = Path.Combine(root, "src", "Fiction");
        if (!Directory.Exists(fictionRoot))
            return seriesList;

        foreach (var universeDir in Directory.GetDirectories(fictionRoot).OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            foreach (var child in Directory.GetDirectories(universeDir).OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                if (!File.Exists(Path.Combine(child, "series.yaml")))
                    continue;
                seriesList.Add(LoadProtocolSeriesDir(child));
            }
        }

        return seriesList;
    }

    static List<BookInfo> LoadProtocolStandaloneBooks(string root)
    {
        var books = new List<BookInfo>();
        var fictionRoot = Path.Combine(root, "src", "Fiction");
        if (Directory.Exists(fictionRoot))
        {
            foreach (var universeDir in Directory.GetDirectories(fictionRoot))
            {
                foreach (var child in Directory.GetDirectories(universeDir))
                {
                    if (File.Exists(Path.Combine(child, "series.yaml")))
                        continue;
                    if (File.Exists(Path.Combine(child, "book.yaml")))
                        books.Add(LoadBook(child, seriesId: null, protocolLayout: true));
                }
            }
        }

        var nonFictionRoot = Path.Combine(root, "src", "NonFiction");
        if (Directory.Exists(nonFictionRoot))
        {
            foreach (var subjectDir in Directory.GetDirectories(nonFictionRoot).OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                foreach (var bookDir in Directory.GetDirectories(subjectDir).OrderBy(Path.GetFileName, StringComparer.Ordinal))
                {
                    if (File.Exists(Path.Combine(bookDir, "book.yaml")))
                        books.Add(LoadBook(bookDir, seriesId: null, protocolLayout: true));
                }
            }
        }

        return books;
    }

    static SeriesInfo LoadProtocolSeriesDir(string seriesDirectory)
    {
        var yaml = BookYaml.LoadFile(Path.Combine(seriesDirectory, "series.yaml"));
        var id = Path.GetFileName(seriesDirectory);
        var title = BookYaml.GetString(yaml, "title")
                    ?? BookYaml.GetString(yaml, "name")
                    ?? id;

        var books = new List<BookInfo>();
        foreach (var bookDir in Directory.GetDirectories(seriesDirectory).OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(bookDir);
            if (name is "References" or "references" or "Assets" or "assets")
                continue;
            if (!File.Exists(Path.Combine(bookDir, "book.yaml")))
                continue;
            books.Add(LoadBook(bookDir, id, protocolLayout: true));
        }

        books = books
            .OrderBy(b => ReadBookOrder(b.DirectoryPath) ?? int.MaxValue)
            .ThenBy(b => b.Id, StringComparer.Ordinal)
            .ToList();

        var references = LoadReferenceSets(seriesDirectory);
        return new SeriesInfo(id, title, seriesDirectory, books, references);
    }

    static SeriesInfo LoadLegacySeries(string seriesDirectory)
    {
        var yaml = BookYaml.LoadFile(Path.Combine(seriesDirectory, "series.yaml"));
        var id = BookYaml.GetString(yaml, "id") ?? Path.GetFileName(seriesDirectory);
        var title = BookYaml.GetString(yaml, "name") ?? BookYaml.GetString(yaml, "title") ?? id;

        var books = new List<BookInfo>();
        var booksDir = Path.Combine(seriesDirectory, "books");
        if (Directory.Exists(booksDir))
        {
            foreach (var bookDir in Directory.GetDirectories(booksDir).OrderBy(Path.GetFileName, StringComparer.Ordinal))
                books.Add(LoadBook(bookDir, id, protocolLayout: false));
        }

        var references = LoadReferenceSets(seriesDirectory);
        return new SeriesInfo(id, title, seriesDirectory, books, references);
    }

    static int? ReadBookOrder(string bookDirectory)
    {
        var yaml = BookYaml.LoadFile(Path.Combine(bookDirectory, "book.yaml"));
        if (yaml.TryGetValue("order", out var raw) && raw is not null)
        {
            if (raw is int i)
                return i;
            if (int.TryParse(raw.ToString(), out var parsed))
                return parsed;
        }

        return null;
    }

    /// <summary>Loads a single book directory into <see cref="BookInfo"/>.</summary>
    public static BookInfo LoadBookDirectory(string bookDirectory, string? seriesId = null, bool? protocolLayout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookDirectory);
        var protocol = protocolLayout
                       ?? (File.Exists(Path.Combine(bookDirectory, "book.yaml"))
                           && (Directory.Exists(Path.Combine(bookDirectory, "Chapters"))
                               || Directory.Exists(Path.Combine(bookDirectory, "chapters"))));
        return LoadBook(bookDirectory, seriesId, protocol);
    }

    internal static BookInfo LoadBook(string bookDirectory, string? seriesId, bool protocolLayout)
    {
        var bookYaml = BookYaml.LoadFile(Path.Combine(bookDirectory, "book.yaml"));
        var id = Path.GetFileName(bookDirectory);
        var title = BookYaml.GetString(bookYaml, "title") ?? id;
        var subtitle = BookYaml.GetString(bookYaml, "subtitle");
        var author = BookYaml.GetString(bookYaml, "author")
                     ?? FirstAuthor(bookYaml)
                     ?? null;
        var orderFromHeading = !protocolLayout && BookYaml.GetBool(bookYaml, "chapter_order_from_heading");
        var debugMode = BookYaml.GetBool(bookYaml, "debug_mode");

        var chapters = new List<ChapterInfo>();
        var chDir = ResolveDir(bookDirectory, protocolLayout ? "Chapters" : "chapters", "chapters", "Chapters");
        if (chDir is not null)
        {
            foreach (var file in Directory.GetFiles(chDir, "*.md"))
            {
                var stem = Path.GetFileNameWithoutExtension(file);
                var sortKey = protocolLayout
                    ? ChapterOrder.GetFilenameSortKey(file)
                    : ChapterOrder.GetSortKey(file);
                chapters.Add(new ChapterInfo(
                    stem,
                    ChapterOrder.ReadChapterTitle(file) ?? stem,
                    ChapterKind.Chapter,
                    sortKey,
                    file));
            }
        }

        var apDir = ResolveDir(bookDirectory, protocolLayout ? "Appendices" : "appendices", "appendices", "Appendices");
        if (apDir is not null)
        {
            var appendixFiles = Directory.GetFiles(apDir, "*.md").OrderBy(Path.GetFileName, StringComparer.Ordinal).ToList();
            for (var i = 0; i < appendixFiles.Count; i++)
            {
                var file = appendixFiles[i];
                var stem = Path.GetFileNameWithoutExtension(file);
                var sortKey = protocolLayout ? ChapterOrder.GetFilenameSortKey(file) : i;
                chapters.Add(new ChapterInfo(
                    stem,
                    ChapterOrder.ReadChapterTitle(file) ?? stem,
                    ChapterKind.Appendix,
                    sortKey,
                    file));
            }
        }

        var ordered = protocolLayout
            ? chapters.OrderBy(c => c.Kind).ThenBy(c => c.SortKey).ThenBy(c => c.FilePath, StringComparer.OrdinalIgnoreCase).ToList()
            : ChapterOrder.SortChapters(chapters, orderFromHeading);
        var references = LoadReferenceSets(bookDirectory);
        return new BookInfo(id, title, subtitle, author, bookDirectory, seriesId, ordered, orderFromHeading, debugMode, references);
    }

    static string? FirstAuthor(Dictionary<string, object?> yaml)
    {
        if (!yaml.TryGetValue("authors", out var raw) || raw is null)
            return null;
        if (raw is System.Collections.IList list && list.Count > 0)
            return list[0]?.ToString();
        return raw.ToString();
    }

    static string? ResolveDir(string parent, string preferred, params string[] fallbacks)
    {
        var preferredPath = Path.Combine(parent, preferred);
        if (Directory.Exists(preferredPath))
            return preferredPath;
        foreach (var name in fallbacks)
        {
            var path = Path.Combine(parent, name);
            if (Directory.Exists(path))
                return path;
        }

        return null;
    }

    static List<ReferenceSetInfo> LoadReferenceSets(string containerDir)
    {
        var result = new List<ReferenceSetInfo>();
        var refRoot = ResolveReferenceRoot(containerDir);
        if (refRoot is null)
            return result;

        var sectionDirs = Directory.GetDirectories(refRoot)
            .Where(d => !Path.GetFileName(d).StartsWith('_'))
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToList();

        if (sectionDirs.Count > 0)
        {
            foreach (var sectionDir in sectionDirs)
            {
                var id = Path.GetFileName(sectionDir);
                var files = CollectReferenceFiles(sectionDir);
                if (files.Count == 0)
                    continue;

                result.Add(new ReferenceSetInfo(id, ToSectionTitle(id), sectionDir, files));
            }
        }
        else
        {
            var files = CollectReferenceFiles(refRoot);
            if (files.Count > 0)
                result.Add(new ReferenceSetInfo("references", "References", refRoot, files));
        }

        return result;
    }

    static string? ResolveReferenceRoot(string containerDir)
    {
        foreach (var name in new[] { "References", "references", "reference" })
        {
            var path = Path.Combine(containerDir, name);
            if (Directory.Exists(path))
                return path;
        }

        return null;
    }

    static List<ReferenceFileInfo> CollectReferenceFiles(string rootDir)
    {
        var files = new List<ReferenceFileInfo>();
        foreach (var path in Directory.GetFiles(rootDir, "*.md", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}_archive{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                continue;

            var stem = Path.GetFileNameWithoutExtension(path);
            var title = ChapterOrder.ReadChapterTitle(path) ?? stem;
            files.Add(new ReferenceFileInfo(stem, title, path));
        }

        files.Sort((a, b) => string.Compare(a.FilePath, b.FilePath, StringComparison.OrdinalIgnoreCase));
        return files;
    }

    static string ToSectionTitle(string folderName) =>
        string.Join(' ', folderName.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
}
