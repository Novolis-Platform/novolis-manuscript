namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Page-break policy for book PDF chapter headings.</summary>
internal static class ChapterPageBreaks
{
    /// <summary>
    /// Page-break before an H1 when the content column already has material
    /// (so chapter 1 after preface, and every later chapter, start on a new page).
    /// </summary>
    public static bool ShouldBreakBeforeHeading(bool contentColumnHasMaterial, int headingLevel) =>
        headingLevel == 1 && contentColumnHasMaterial;
}
