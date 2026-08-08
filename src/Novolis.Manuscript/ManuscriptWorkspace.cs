using Novolis.IO.Paths;

namespace Novolis.Manuscript;

/// <summary>An opened manuscript content workspace.</summary>
public sealed class ManuscriptWorkspace
{
    ManuscriptWorkspace(string contentRoot)
    {
        ContentRoot = contentRoot;
        Catalog = new ManuscriptCatalog();
    }

    /// <summary>Absolute content root (contains <c>content/</c>).</summary>
    public string ContentRoot { get; }

    /// <summary>Catalog loader for this workspace.</summary>
    public ManuscriptCatalog Catalog { get; }

    /// <summary>
    /// Tries to open a workspace by walking parents for <c>content/series</c> or <c>content/books</c>.
    /// </summary>
    public static bool TryOpen(string startDir, out ManuscriptWorkspace? workspace)
    {
        workspace = null;
        if (string.IsNullOrWhiteSpace(startDir) || !Directory.Exists(startDir))
            return false;

        if (RootFinder.TryFind(startDir, ["content/series"], out var root)
            || RootFinder.TryFind(startDir, ["content/books"], out root))
        {
            workspace = new ManuscriptWorkspace(Path.GetFullPath(root));
            return true;
        }

        // Direct content root that already is the workspace folder.
        var series = Path.Combine(startDir, "content", "series");
        var books = Path.Combine(startDir, "content", "books");
        if (Directory.Exists(series) || Directory.Exists(books))
        {
            workspace = new ManuscriptWorkspace(Path.GetFullPath(startDir));
            return true;
        }

        return false;
    }
}
