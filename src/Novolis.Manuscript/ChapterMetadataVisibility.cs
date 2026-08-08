namespace Novolis.Manuscript;

/// <summary>Which chapter-metadata fields appear in reader-facing builds.</summary>
public static class ChapterMetadataVisibility
{
    static readonly HashSet<string> PublicTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "date", "time", "system", "location", "locations",
    };

    /// <summary>Reader-facing tags (dateline / place anchors).</summary>
    public static bool IsPublicTag(string tag) =>
        !string.IsNullOrWhiteSpace(tag) && PublicTags.Contains(tag.Trim());

    /// <summary>Authoring-only tags (POV, cast, status, notes, …).</summary>
    public static bool IsHiddenTag(string tag) => !IsPublicTag(tag);
}
