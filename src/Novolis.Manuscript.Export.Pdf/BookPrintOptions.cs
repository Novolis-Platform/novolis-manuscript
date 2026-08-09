namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Options for book-folder and multi-format print export.</summary>
public sealed class BookPrintOptions
{
    /// <summary>
    /// When true, emit every <c>[!tag]</c> chapter-metadata line in PDF/HTML/TXT
    /// (overrides per-book <c>debug_mode</c>).
    /// </summary>
    public bool DebugMode { get; set; }

    /// <summary>Optional JSON path for typography overrides (books print-settings shape).</summary>
    public string? PrintSettingsPath { get; set; }

    /// <summary>Explicit settings; when null, loaded from <see cref="PrintSettingsPath"/> or defaults.</summary>
    public ManuscriptPrintSettings? Settings { get; set; }

    /// <summary>Override cover page; null uses <see cref="ManuscriptPrintSettings.IncludeCover"/>.</summary>
    public bool? IncludeCover { get; set; }

    /// <summary>
    /// When true, show all chapter-metadata tags in reader builds.
    /// Defaults to the same behavior as <see cref="DebugMode"/> when left false and debug is on.
    /// </summary>
    public bool ShowAllMetadataTags { get; set; }

    /// <summary>Optional series display name for the cover (overrides series.yaml title).</summary>
    public string? SeriesTitle { get; set; }

    /// <summary>Optional rights/copyright line on the cover.</summary>
    public string? Rights { get; set; }

    /// <summary>Resolves effective print settings from this options instance and book path.</summary>
    /// <param name="bookDirectory">Book folder; used to pick fiction vs textbook profile when <see cref="Settings"/> is null.</param>
    public ManuscriptPrintSettings ResolveSettings(string? bookDirectory = null)
    {
        var settings = Settings
                       ?? ManuscriptPrintSettings.ResolveForDirectory(bookDirectory, PrintSettingsPath);
        if (IncludeCover is { } cover)
            settings.IncludeCover = cover;
        return settings;
    }

    /// <summary>Whether chapter-metadata filtering should include all tags.</summary>
    public bool ResolveShowAllMetadataTags(bool bookDebugMode) =>
        DebugMode || ShowAllMetadataTags || bookDebugMode;
}
