namespace Novolis.Manuscript.Export.Markdown;

/// <summary>HTML companion theme (GitHub-flavored Markdown CSS, light or dark).</summary>
public enum ManuscriptHtmlTheme
{
    /// <summary>GitHub light theme.</summary>
    GitHubLight,

    /// <summary>GitHub dark theme.</summary>
    GitHubDark,
}

/// <summary>Options for manuscript Markdown / HTML export.</summary>
public sealed class ManuscriptMarkdownExportOptions
{
    /// <summary>Also write <c>{id}.author.md</c> with hidden metadata callouts.</summary>
    public bool IncludeAuthorMarkdown { get; set; } = true;

    /// <summary>Write HTML companion from reader markdown.</summary>
    public bool IncludeHtml { get; set; } = true;

    /// <summary>HTML theme for companion.</summary>
    public ManuscriptHtmlTheme HtmlTheme { get; set; } = ManuscriptHtmlTheme.GitHubLight;

    /// <summary>Force author mode even when book debug is off.</summary>
    public bool AuthorMode { get; set; }

    /// <summary>Optional series title override for cover/meta.</summary>
    public string? SeriesTitle { get; set; }

    /// <summary>Optional rights line (unused in MD body; reserved for hosts).</summary>
    public string? Rights { get; set; }
}

/// <summary>Paths written by <see cref="ManuscriptMarkdownExporter"/>.</summary>
public sealed record ManuscriptMarkdownPaths(
    string ReaderMarkdownPath,
    string? AuthorMarkdownPath,
    string? HtmlPath);
