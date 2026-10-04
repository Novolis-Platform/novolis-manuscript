using Novolis.Markup.Markdown;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>
/// Converts Markdown sections that do not have a useful speech representation
/// into short, deterministic spoken placeholders.
/// </summary>
public static class MarkdownDeserializer
{
    /// <summary>Spoken substitute when a table cannot be narrated.</summary>
    public const string TablePlaceholder = "Table cannot be read.";

    /// <summary>Spoken substitute when a code block cannot be narrated.</summary>
    public const string CodeBlockPlaceholder = "Code-block cannot be read.";

    /// <summary>Returns a speech placeholder for tables and code blocks; otherwise empty.</summary>
    public static string ToSpeech(IMarkdownSection section) =>
        section switch
        {
            IMarkdownTable => TablePlaceholder,
            IMarkdownCodeBlock => CodeBlockPlaceholder,
            _ => string.Empty,
        };
}
