using Novolis.Manuscript;

namespace Novolis.Manuscript.Metrics;

/// <summary>Stable finding codes for chapter metadata quality (not structural Doctor).</summary>
public static class MetadataDebtCodes
{
    /// <summary>Literal <c>TK</c> placeholder in a metadata field.</summary>
    public const string MetadataTk = "metadata-tk";

    /// <summary>Chapter has metadata block but missing <c>pov</c>.</summary>
    public const string MissingPov = "metadata-missing-pov";

    /// <summary>Chapter has metadata block but missing <c>characters</c>.</summary>
    public const string MissingCharacters = "metadata-missing-characters";
}
