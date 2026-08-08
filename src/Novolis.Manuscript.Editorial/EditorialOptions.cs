namespace Novolis.Manuscript.Editorial;

/// <summary>Options for <see cref="EditorialAnalyzer"/>.</summary>
public sealed class EditorialOptions
{
    /// <summary>Rule profile. Default <see cref="EditorialProfile.Fiction"/> (neutral).</summary>
    public EditorialProfile Profile { get; init; } = EditorialProfile.Fiction;

    /// <summary>Run lexicon forbid / prefer rules. Default false unless set or Calypso profile.</summary>
    public bool? EnableLexicon { get; init; }

    /// <summary>Run AI-slop pattern rules. Default true.</summary>
    public bool EnableSlop { get; init; } = true;

    /// <summary>Run naming-variant rules. Default true.</summary>
    public bool EnableNaming { get; init; } = true;

    /// <summary>Variant→canonical name pairs (no built-in cast table).</summary>
    public IReadOnlyDictionary<string, string>? ExtraNames { get; init; }

    /// <summary>Forbidden lexicon phrases (empty = no forbid hits).</summary>
    public IReadOnlyList<string>? ForbiddenPhrases { get; init; }

    /// <summary>Prefer-pair lexicon entries.</summary>
    public IReadOnlyList<(string Flagged, string Prefer)>? PreferPairs { get; init; }

    /// <summary>Effective lexicon enablement.</summary>
    public bool LexiconEnabled =>
        EnableLexicon ?? Profile == EditorialProfile.Calypso;
}
