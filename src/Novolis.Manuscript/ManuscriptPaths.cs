namespace Novolis.Manuscript;

/// <summary>Resolves chapter directories and book folders for NMP/1 and legacy layouts.</summary>
public static class ManuscriptPaths
{
    /// <summary>Resolves the chapters directory under a book folder (<c>Chapters</c> or <c>chapters</c>).</summary>
    public static string ResolveChaptersDirectory(string bookDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookDirectory);
        var full = Path.GetFullPath(bookDirectory);
        foreach (var name in new[] { "Chapters", "chapters" })
        {
            var path = Path.Combine(full, name);
            if (Directory.Exists(path))
                return path;
        }

        throw new DirectoryNotFoundException($"Chapters directory not found under {full}");
    }

    /// <summary>Resolves chapters dir from an already-loaded <see cref="BookInfo"/>.</summary>
    public static string ResolveChaptersDirectory(BookInfo book)
    {
        ArgumentNullException.ThrowIfNull(book);
        return ResolveChaptersDirectory(book.DirectoryPath);
    }

    /// <summary>Finds a book and its chapters directory in a workspace.</summary>
    public static (BookInfo Book, string ChaptersDir) ResolveBookChapters(
        string workspaceRoot,
        string? seriesId,
        string bookId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(bookId);
        if (!ManuscriptWorkspace.TryOpen(workspaceRoot, out var ws) || ws is null)
            throw new InvalidOperationException($"Not a manuscript workspace: {workspaceRoot}");

        var book = ws.Catalog.FindBook(ws.ContentRoot, seriesId, bookId)
                   ?? throw new FileNotFoundException($"Book not found: {seriesId}/{bookId}");
        return (book, ResolveChaptersDirectory(book));
    }

    /// <summary>Resolves chapters directory from a <c>book.yaml</c> path.</summary>
    public static string ResolveChaptersDirectoryFromBookYaml(string bookYamlPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookYamlPath);
        var bookDir = Path.GetDirectoryName(Path.GetFullPath(bookYamlPath))
                      ?? throw new InvalidOperationException($"Invalid book.yaml path: {bookYamlPath}");
        return ResolveChaptersDirectory(bookDir);
    }
}
