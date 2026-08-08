using System.Diagnostics.CodeAnalysis;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Locates <c>style.css</c> by walking up from a book or series directory.</summary>
[ExcludeFromCodeCoverage(Justification = "Filesystem walk orthogonal to print remodel.")]
internal static class StylesheetLocator
{
    /// <summary>
    /// Walks from <paramref name="startDirectory"/> toward the filesystem root (or optional stop)
    /// looking for <c>style.css</c>, then tries <c>style/style.css</c> under <paramref name="contentRootHint"/>.
    /// </summary>
    public static string? Find(string? startDirectory, string? contentRootHint = null, string? stopAt = null)
    {
        string? WalkUp(string? start)
        {
            if (string.IsNullOrEmpty(start) || !Directory.Exists(start))
                return null;
            var path = Path.GetFullPath(start);
            var stop = string.IsNullOrEmpty(stopAt) ? null : Path.GetFullPath(stopAt);
            while (true)
            {
                var candidate = Path.Combine(path, "style.css");
                if (File.Exists(candidate))
                    return candidate;
                if (stop != null && string.Equals(path, stop, StringComparison.OrdinalIgnoreCase))
                    break;
                var parent = Directory.GetParent(path);
                if (parent == null)
                    break;
                path = parent.FullName;
            }

            return null;
        }

        var found = WalkUp(startDirectory);
        if (found != null)
            return found;

        if (!string.IsNullOrEmpty(contentRootHint))
        {
            found = WalkUp(contentRootHint);
            if (found != null)
                return found;

            var globalStyle = Path.Combine(contentRootHint, "style", "style.css");
            if (File.Exists(globalStyle))
                return Path.GetFullPath(globalStyle);
        }

        return null;
    }
}
