using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace Novolis.Manuscript;

/// <summary>Parsed chapter metadata fields.</summary>
public sealed class ManuscriptChapterMetadata
{
    /// <summary>Chapter number string.</summary>
    public string? Number { get; set; }

    /// <summary>Chapter title.</summary>
    public string? Title { get; set; }

    /// <summary>Date field.</summary>
    public string? Date { get; set; }

    /// <summary>Time field.</summary>
    public string? Time { get; set; }

    /// <summary>System / location volume.</summary>
    public string? System { get; set; }

    /// <summary>Location.</summary>
    public string? Location { get; set; }

    /// <summary>Point of view.</summary>
    public string? Pov { get; set; }

    /// <summary>Characters list.</summary>
    public string? Characters { get; set; }

    /// <summary>Status.</summary>
    public string? Status { get; set; }

    /// <summary>Notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Additional unknown callout keys.</summary>
    public Dictionary<string, string> Extra { get; } = new(StringComparer.OrdinalIgnoreCase);
}
