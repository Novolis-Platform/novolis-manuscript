using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace Novolis.Manuscript;

/// <summary>Known metadata format for a chapter document.</summary>
public enum ManuscriptMetadataFormat
{
    /// <summary>No recognized metadata block.</summary>
    None,
    /// <summary>Obsidian-style <c>&gt; [!tag]</c> callouts.</summary>
    Callout,
    /// <summary>YAML front matter between <c>---</c> fences.</summary>
    Yaml
}
