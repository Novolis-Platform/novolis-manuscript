using System.Text.RegularExpressions;
using Novolis.Markup.Markdown;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Which <c>[!tag]</c> lines appear in reader-facing builds.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Thin delegate to ChapterMetadataVisibility.")]
internal static class ChapterMetadataTagVisibility
{
    public static bool IsPublicTag(string tag) =>
        string.Equals(tag, "line", StringComparison.OrdinalIgnoreCase)
        || Novolis.Manuscript.ChapterMetadataVisibility.IsPublicTag(tag);

    public static List<(string Tag, string Value)> FilterForBuild(
        List<(string Tag, string Value)> rows,
        bool showAllTags)
    {
        IEnumerable<(string Tag, string Value)> q = rows.Where(r => !string.IsNullOrWhiteSpace(r.Value));
        if (!showAllTags)
            q = q.Where(r => IsPublicTag(r.Tag));
        return q.ToList();
    }
}
