using Novolis.IO.Paths;

namespace Novolis.Manuscript;

/// <summary>An opened manuscript content workspace.</summary>
public sealed class ManuscriptWorkspace
{
    ManuscriptWorkspace(string contentRoot, bool isProtocolLayout)
    {
        ContentRoot = contentRoot;
        IsProtocolLayout = isProtocolLayout;
        Catalog = new ManuscriptCatalog();
    }

    /// <summary>
    /// Absolute workspace root. For NMP/1 this is the directory containing <c>manuscript.yaml</c>;
    /// for legacy trees it is the folder that contains <c>content/</c>.
    /// </summary>
    public string ContentRoot { get; }

    /// <summary>True when this workspace is an NMP/1 tree (<c>manuscript.yaml</c> + <c>src/</c>).</summary>
    public bool IsProtocolLayout { get; }

    /// <summary>Catalog loader for this workspace.</summary>
    public ManuscriptCatalog Catalog { get; }

    /// <summary>
    /// Tries to open a workspace by walking parents for <c>manuscript.yaml</c> / NMP <c>src/</c>
    /// or legacy <c>content/series</c> / <c>content/books</c>.
    /// </summary>
    public static bool TryOpen(string startDir, out ManuscriptWorkspace? workspace)
    {
        workspace = null;
        if (string.IsNullOrWhiteSpace(startDir) || !Directory.Exists(startDir))
            return false;

        if (RootFinder.TryFind(startDir, IsNmpRoot, out var nmpRoot))
        {
            workspace = new ManuscriptWorkspace(Path.GetFullPath(nmpRoot), isProtocolLayout: true);
            return true;
        }

        if (RootFinder.TryFind(startDir, ["content/series"], out var rootLegacy)
            || RootFinder.TryFind(startDir, ["content/books"], out rootLegacy))
        {
            workspace = new ManuscriptWorkspace(Path.GetFullPath(rootLegacy!), isProtocolLayout: false);
            return true;
        }

        var series = Path.Combine(startDir, "content", "series");
        var books = Path.Combine(startDir, "content", "books");
        if (Directory.Exists(series) || Directory.Exists(books))
        {
            workspace = new ManuscriptWorkspace(Path.GetFullPath(startDir), isProtocolLayout: false);
            return true;
        }

        return false;
    }

    static bool IsNmpRoot(DirectoryInfo dir)
    {
        var path = dir.FullName;
        if (File.Exists(Path.Combine(path, "manuscript.yaml")))
            return true;
        var src = Path.Combine(path, "src");
        return Directory.Exists(Path.Combine(src, "Fiction"))
               || Directory.Exists(Path.Combine(src, "NonFiction"));
    }
}
