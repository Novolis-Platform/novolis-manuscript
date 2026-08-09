using System.Text.Json;
using System.Text.Json.Serialization;

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

    /// <summary>Generic heading font size in points (chapter/H2/H3 sizes below take precedence for PDF).</summary>
    public float HeadingFontSize { get; set; } = 14f;

    /// <summary>Body font family.</summary>
    public string BodyFontFamily { get; set; } = "Georgia";

    /// <summary>Code font family.</summary>
    public string CodeFontFamily { get; set; } = "Courier New";

    /// <summary>Whether to include a cover page.</summary>
    public bool IncludeCover { get; set; } = true;

    /// <summary>
    /// When true, emit plain public dateline <c>&gt;</c> lines under each H1 (fiction).
    /// NonFiction / textbook profile sets this false.
    /// </summary>
    public bool IncludePublicDateline { get; set; } = true;

    /// <summary>
    /// When true, PDF mapping uses textbook code chrome and labeled admonition panels.
    /// </summary>
    public bool UseTextbookChrome { get; set; }

    /// <summary>Body line height multiplier.</summary>
    public float LineHeight { get; set; } = 1.42f;

    /// <summary>Spacing between block items in points.</summary>
    public float ParagraphSpacingPt { get; set; } = 8f;

    /// <summary>Chapter (H1) title size in points.</summary>
    public float ChapterTitleSizePt { get; set; } = 19f;

    /// <summary>H2 size in points.</summary>
    public float H2SizePt { get; set; } = 14f;

    /// <summary>H3 size in points.</summary>
    public float H3SizePt { get; set; } = 12f;

    /// <summary>H4 size in points (textbooks).</summary>
    public float H4SizePt { get; set; } = 11f;

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

    /// <summary>
    /// Letter-page textbook defaults (NonFiction): no fiction dateline, pedagogical chrome on.
    /// </summary>
    public static ManuscriptPrintSettings ForTextbook() => new()
    {
        PageWidthInches = 8.5f,
        PageHeightInches = 11f,
        MarginHorizontalInches = 1f,
        MarginRightInches = 1f,
        MarginVerticalInches = 1f,
        BodyFontSize = 11f,
        HeadingFontSize = 16f,
        BodyFontFamily = "Georgia",
        CodeFontFamily = "Consolas",
        IncludeCover = true,
        IncludePublicDateline = false,
        UseTextbookChrome = true,
        LineHeight = 1.6f,
        ParagraphSpacingPt = 8f,
        ChapterTitleSizePt = 20f,
        H2SizePt = 15f,
        H3SizePt = 12.5f,
        H4SizePt = 11f,
        SceneBreakSizePt = 18f,
    };

    /// <summary>True when <paramref name="path"/> sits under <c>src/NonFiction/</c>.</summary>
    public static bool IsNonFictionBookPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var full = Path.GetFullPath(path);
        var parts = full.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i].Equals("src", StringComparison.OrdinalIgnoreCase)
                && parts[i + 1].Equals("NonFiction", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Fiction or textbook seed for <paramref name="directoryPath"/>, then optional <c>print.json</c> property overlays.
    /// </summary>
    public static ManuscriptPrintSettings ResolveForDirectory(string? directoryPath, string? printSettingsPath = null)
    {
        var settings = IsNonFictionBookPath(directoryPath) ? ForTextbook() : new ManuscriptPrintSettings();
        if (!string.IsNullOrWhiteSpace(printSettingsPath) && File.Exists(printSettingsPath))
            ApplyJsonOverrides(settings, File.ReadAllText(printSettingsPath));
        return settings;
    }

    /// <summary>Loads settings from JSON (missing file → fiction defaults).</summary>
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

    static void ApplyJsonOverrides(ManuscriptPrintSettings target, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return;

        SetFloat(root, "pageWidthInches", v => target.PageWidthInches = v);
        SetFloat(root, "pageHeightInches", v => target.PageHeightInches = v);
        SetFloat(root, "marginHorizontalInches", v => target.MarginHorizontalInches = v);
        SetFloat(root, "marginRightInches", v => target.MarginRightInches = v);
        SetFloat(root, "marginVerticalInches", v => target.MarginVerticalInches = v);
        SetFloat(root, "bodyFontSize", v => target.BodyFontSize = v);
        SetFloat(root, "bodyFontSizePt", v => target.BodyFontSize = v);
        SetFloat(root, "headingFontSize", v => target.HeadingFontSize = v);
        SetFloat(root, "lineHeight", v => target.LineHeight = v);
        SetFloat(root, "paragraphSpacingPt", v => target.ParagraphSpacingPt = v);
        SetFloat(root, "chapterTitleSizePt", v => target.ChapterTitleSizePt = v);
        SetFloat(root, "h2SizePt", v => target.H2SizePt = v);
        SetFloat(root, "h3SizePt", v => target.H3SizePt = v);
        SetFloat(root, "h4SizePt", v => target.H4SizePt = v);
        SetFloat(root, "sceneBreakSizePt", v => target.SceneBreakSizePt = v);
        SetString(root, "bodyFontFamily", v => target.BodyFontFamily = v);
        SetString(root, "fontFamily", v => target.BodyFontFamily = v);
        SetString(root, "codeFontFamily", v => target.CodeFontFamily = v);
        SetBool(root, "includeCover", v => target.IncludeCover = v);
        SetBool(root, "includePublicDateline", v => target.IncludePublicDateline = v);
        SetBool(root, "useTextbookChrome", v => target.UseTextbookChrome = v);
    }

    static void SetFloat(JsonElement root, string name, Action<float> apply)
    {
        if (!TryGetProperty(root, name, out var el))
            return;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetSingle(out var v))
            apply(v);
    }

    static void SetString(JsonElement root, string name, Action<string> apply)
    {
        if (!TryGetProperty(root, name, out var el))
            return;
        if (el.ValueKind == JsonValueKind.String)
        {
            var s = el.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                apply(s);
        }
    }

    static void SetBool(JsonElement root, string name, Action<bool> apply)
    {
        if (!TryGetProperty(root, name, out var el))
            return;
        if (el.ValueKind is JsonValueKind.True or JsonValueKind.False)
            apply(el.GetBoolean());
    }

    static bool TryGetProperty(JsonElement root, string name, out JsonElement el)
    {
        if (root.TryGetProperty(name, out el))
            return true;
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                el = prop.Value;
                return true;
            }
        }

        el = default;
        return false;
    }
}
