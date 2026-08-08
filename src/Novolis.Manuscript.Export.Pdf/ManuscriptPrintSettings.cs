using System.Text.Json;
using System.Text.Json.Serialization;
using Novolis.Markup.Markdown.Rendering;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Print layout and typography settings for manuscript PDF export.</summary>
public sealed class ManuscriptPrintSettings
{
    /// <summary>Page width in inches (default trade paperback 6).</summary>
    public float PageWidthInches { get; set; } = 6f;

    /// <summary>Page height in inches (default trade paperback 9).</summary>
    public float PageHeightInches { get; set; } = 9f;

    /// <summary>Horizontal margin in inches (left).</summary>
    public float MarginHorizontalInches { get; set; } = 0.65f;

    /// <summary>Right margin in inches (books print uses a slightly tighter right edge).</summary>
    public float MarginRightInches { get; set; } = 0.55f;

    /// <summary>Vertical margin in inches.</summary>
    public float MarginVerticalInches { get; set; } = 0.75f;

    /// <summary>Body font size in points.</summary>
    public float BodyFontSize { get; set; } = 11f;

    /// <summary>Heading font size in points (legacy Markup path; QuestPDF uses chapter/H2/H3 sizes).</summary>
    public float HeadingFontSize { get; set; } = 14f;

    /// <summary>Body font family.</summary>
    public string BodyFontFamily { get; set; } = "Georgia";

    /// <summary>Code font family.</summary>
    public string CodeFontFamily { get; set; } = "Courier New";

    /// <summary>Whether to include a cover page.</summary>
    public bool IncludeCover { get; set; } = true;

    /// <summary>Body line height multiplier.</summary>
    public float LineHeight { get; set; } = 1.42f;

    /// <summary>Spacing between block items in the QuestPDF column.</summary>
    public float ParagraphSpacingPt { get; set; } = 8f;

    /// <summary>Chapter (H1) title size in points.</summary>
    public float ChapterTitleSizePt { get; set; } = 19f;

    /// <summary>H2 size in points.</summary>
    public float H2SizePt { get; set; } = 14f;

    /// <summary>H3+ size in points.</summary>
    public float H3SizePt { get; set; } = 12f;

    /// <summary>Scene-break (<c>***</c>) glyph size.</summary>
    public float SceneBreakSizePt { get; set; } = 22f;

    /// <summary>Alias used by books print-settings JSON (<c>fontFamily</c>).</summary>
    [JsonPropertyName("fontFamily")]
    public string? FontFamily
    {
        get => BodyFontFamily;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
                BodyFontFamily = value;
        }
    }

    /// <summary>Alias used by books print-settings JSON (<c>bodyFontSizePt</c>).</summary>
    [JsonPropertyName("bodyFontSizePt")]
    public float? BodyFontSizePt
    {
        get => BodyFontSize;
        set
        {
            if (value is { } v)
                BodyFontSize = v;
        }
    }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Loads settings from JSON (missing file → defaults).</summary>
    public static ManuscriptPrintSettings Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new ManuscriptPrintSettings();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ManuscriptPrintSettings>(json, JsonOptions) ?? new ManuscriptPrintSettings();
        }
        catch
        {
            return new ManuscriptPrintSettings();
        }
    }

    /// <summary>Saves settings as JSON.</summary>
    public void Save(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }

    /// <summary>Maps to <see cref="MarkdownPdfExportOptions"/> (Markup fallback path).</summary>
    public MarkdownPdfExportOptions ToPdfOptions(string? title, string? subtitle, string? author) =>
        new()
        {
            Title = title,
            Subtitle = subtitle,
            Author = author,
            IncludeCoverPage = IncludeCover,
            PageWidthInches = PageWidthInches,
            PageHeightInches = PageHeightInches,
            MarginHorizontalInches = MarginHorizontalInches,
            MarginVerticalInches = MarginVerticalInches,
            BodyFontSize = BodyFontSize,
            HeadingFontSize = HeadingFontSize,
            BodyFontFamily = BodyFontFamily,
            CodeFontFamily = CodeFontFamily,
        };
}
