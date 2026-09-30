using Novolis.Markup.Markdown;
using ProtocolWorkspace = Novolis.Manuscript.Protocol.ManuscriptWorkspace;

namespace Novolis.Manuscript;

/// <summary>
/// A book of ordered chapter files. Each chapter body is parsed once as
/// <see cref="IMarkdownDocument"/> (YAML front matter stripped, dateline quotes kept).
/// </summary>
public sealed class BookDocument
{
    BookDocument(
        string id,
        string title,
        string? subtitle,
        string? author,
        string? seriesId,
        string? seriesTitle,
        string? rights,
        string directoryPath,
        IReadOnlyList<ChapterDocument> chapters)
    {
        Id = id;
        Title = title;
        Subtitle = subtitle;
        Author = author;
        SeriesId = seriesId;
        SeriesTitle = seriesTitle;
        Rights = rights;
        DirectoryPath = directoryPath;
        Chapters = chapters;
    }

    /// <summary>Book directory id.</summary>
    public string Id { get; }

    /// <summary>Display title.</summary>
    public string Title { get; }

    /// <summary>Optional subtitle.</summary>
    public string? Subtitle { get; }

    /// <summary>Author line (first author when the protocol lists several).</summary>
    public string? Author { get; }

    /// <summary>Series id, or null for a standalone book.</summary>
    public string? SeriesId { get; }

    /// <summary>Series display title when this book sits in a series.</summary>
    public string? SeriesTitle { get; }

    /// <summary>Rights / copyright line.</summary>
    public string? Rights { get; }

    /// <summary>Absolute book directory.</summary>
    public string DirectoryPath { get; }

    /// <summary>Chapters then appendices, in protocol or filename order.</summary>
    public IReadOnlyList<ChapterDocument> Chapters { get; }

    /// <summary>
    /// Opens a book directory. Uses NMP/1 when <c>manuscript.yaml</c> sits above the
    /// directory; otherwise scans <c>Chapters/</c> / <c>chapters/</c> as a legacy tree.
    /// </summary>
    public static BookDocument Open(string bookDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookDirectory);
        var dir = Path.GetFullPath(bookDirectory);
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Book directory not found: {dir}");

        if (TryFindProtocolRoot(dir, out var root))
            return OpenProtocol(root, dir);

        return OpenLegacy(dir);
    }

    static BookDocument OpenProtocol(string workspaceRoot, string bookDirectory)
    {
        var snapshot = ProtocolWorkspace.Open(workspaceRoot).Read();
        foreach (var universe in snapshot.Catalog.Fiction)
        {
            foreach (var series in universe.Series)
            {
                foreach (var book in series.Books)
                {
                    var path = BookPath(workspaceRoot, book.Address);
                    if (!PathsEqual(path, bookDirectory))
                        continue;
                    return FromProtocol(book, series.Id, series.Metadata.Title, path);
                }
            }

            foreach (var book in universe.Books)
            {
                var path = BookPath(workspaceRoot, book.Address);
                if (!PathsEqual(path, bookDirectory))
                    continue;
                return FromProtocol(book, seriesId: null, seriesTitle: null, path);
            }
        }

        foreach (var subject in snapshot.Catalog.NonFiction)
        {
            foreach (var book in subject.Books)
            {
                var path = BookPath(workspaceRoot, book.Address);
                if (!PathsEqual(path, bookDirectory))
                    continue;
                return FromProtocol(book, seriesId: null, seriesTitle: null, path);
            }
        }

        throw new FileNotFoundException($"Book not found in manuscript catalog: {bookDirectory}");
    }

    static BookDocument FromProtocol(
        Protocol.ManuscriptBook book,
        string? seriesId,
        string? seriesTitle,
        string directoryPath)
    {
        var chapters = new List<ChapterDocument>();
        foreach (var doc in book.Chapters)
            chapters.Add(LoadChapter(doc.Order, doc.Slug, doc.Title, ChapterKind.Chapter, doc.FilePath));
        foreach (var doc in book.Appendices)
            chapters.Add(LoadChapter(doc.Order, doc.Slug, doc.Title, ChapterKind.Appendix, doc.FilePath));

        var authors = book.Metadata.Authors;
        var author = authors is { Count: > 0 } ? authors[0] : null;
        return new BookDocument(
            book.Address.BookId,
            book.Metadata.Title,
            book.Metadata.Subtitle,
            author,
            seriesId,
            seriesTitle,
            book.Metadata.Rights,
            directoryPath,
            chapters);
    }

    static BookDocument OpenLegacy(string bookDirectory)
    {
        var id = Path.GetFileName(bookDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var yaml = BookYaml.LoadFile(Path.Combine(bookDirectory, "book.yaml"));
        var title = BookYaml.GetString(yaml, "title") ?? id;
        var subtitle = BookYaml.GetString(yaml, "subtitle");
        var author = BookYaml.GetString(yaml, "author") ?? FirstAuthor(yaml);
        var rights = BookYaml.GetString(yaml, "rights") ?? BookYaml.GetString(yaml, "copyright");
        var seriesId = BookYaml.GetString(yaml, "series");
        var seriesTitle = ResolveLegacySeriesTitle(bookDirectory) ?? seriesId;

        var chapters = new List<ChapterDocument>();
        var chDir = ResolveDir(bookDirectory, "Chapters", "chapters");
        if (chDir is not null)
        {
            foreach (var file in Directory.GetFiles(chDir, "*.md").OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                var stem = Path.GetFileNameWithoutExtension(file);
                var order = ParseOrderPrefix(stem);
                chapters.Add(LoadChapter(order, stem, titleHint: null, ChapterKind.Chapter, file));
            }
        }

        var apDir = ResolveDir(bookDirectory, "Appendices", "appendices");
        if (apDir is not null)
        {
            foreach (var file in Directory.GetFiles(apDir, "*.md").OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                var stem = Path.GetFileNameWithoutExtension(file);
                chapters.Add(LoadChapter(chapters.Count + 1, stem, titleHint: null, ChapterKind.Appendix, file));
            }
        }

        var ordered = chapters
            .OrderBy(c => c.Kind)
            .ThenBy(c => c.Order)
            .ThenBy(c => c.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new BookDocument(id, title, subtitle, author, seriesId, seriesTitle, rights, bookDirectory, ordered);
    }

    static ChapterDocument LoadChapter(
        int order,
        string slug,
        string? titleHint,
        ChapterKind kind,
        string filePath)
    {
        var text = File.ReadAllText(filePath);
        if (text.StartsWith('\uFEFF'))
            text = text[1..];
        var bodyMarkdown = StripFrontMatter(text);
        var body = MarkdownDocument.Parse(bodyMarkdown);
        var title = titleHint;
        if (string.IsNullOrWhiteSpace(title))
            title = FirstHeading(body) ?? slug;
        return new ChapterDocument(order, slug, title, kind, Path.GetFullPath(filePath), body);
    }

    static string StripFrontMatter(string text)
    {
        var normalized = text.Replace("\r\n", "\n");
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
            return text;

        var end = normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (end < 0)
        {
            end = normalized.IndexOf("\n---", 4, StringComparison.Ordinal);
            if (end < 0 || end + 4 != normalized.Length)
                return text;
            return string.Empty;
        }

        return normalized[(end + 5)..];
    }

    static string? FirstHeading(IMarkdownDocument document)
    {
        foreach (var section in document)
        {
            if (section is IMarkdownHeader { Level: 1, Text: { Length: > 0 } text })
                return text.Trim();
        }

        return null;
    }

    static string BookPath(string workspaceRoot, Protocol.ManuscriptAddress address)
    {
        var kind = address.Kind == Protocol.ManuscriptKind.Fiction ? "Fiction" : "NonFiction";
        if (string.IsNullOrWhiteSpace(address.SeriesId))
            return Path.GetFullPath(Path.Combine(workspaceRoot, "src", kind, address.ScopeId, address.BookId));
        return Path.GetFullPath(Path.Combine(
            workspaceRoot, "src", kind, address.ScopeId, address.SeriesId, address.BookId));
    }

    static bool TryFindProtocolRoot(string startDir, out string root)
    {
        var current = startDir;
        while (true)
        {
            if (File.Exists(Path.Combine(current, "manuscript.yaml")))
            {
                root = current;
                return true;
            }

            var parent = Directory.GetParent(current);
            if (parent is null)
            {
                root = "";
                return false;
            }

            current = parent.FullName;
        }
    }

    static bool PathsEqual(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    static string? ResolveDir(string parent, params string[] names)
    {
        foreach (var name in names)
        {
            var path = Path.Combine(parent, name);
            if (Directory.Exists(path))
                return path;
        }

        return null;
    }

    static int ParseOrderPrefix(string stem)
    {
        var i = 0;
        while (i < stem.Length && char.IsDigit(stem[i]))
            i++;
        if (i == 0)
            return int.MaxValue;
        return int.TryParse(stem[..i], System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var n)
            ? n
            : int.MaxValue;
    }

    static string? FirstAuthor(Dictionary<string, object?> yaml)
    {
        if (!yaml.TryGetValue("authors", out var raw) || raw is null)
            return null;
        if (raw is System.Collections.IList list && list.Count > 0)
            return list[0]?.ToString()?.Trim();
        var s = raw.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    static string? ResolveLegacySeriesTitle(string bookDirectory)
    {
        var parent = Directory.GetParent(bookDirectory)?.FullName;
        if (parent is null)
            return null;
        var seriesYaml = Path.Combine(parent, "series.yaml");
        if (!File.Exists(seriesYaml))
        {
            var booksParent = Directory.GetParent(parent)?.FullName;
            if (booksParent is not null)
                seriesYaml = Path.Combine(booksParent, "series.yaml");
        }

        if (!File.Exists(seriesYaml))
            return null;
        var yaml = BookYaml.LoadFile(seriesYaml);
        return BookYaml.GetString(yaml, "title") ?? BookYaml.GetString(yaml, "name");
    }
}
