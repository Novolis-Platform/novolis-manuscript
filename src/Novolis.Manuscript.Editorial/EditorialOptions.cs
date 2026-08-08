namespace Novolis.Manuscript.Editorial;

/// <summary>Options for <see cref="EditorialAnalyzer"/>.</summary>
public sealed class EditorialOptions
{
    /// <summary>Rule profile. Default <see cref="EditorialProfile.Fiction"/>.</summary>
    public EditorialProfile Profile { get; init; } = EditorialProfile.Fiction;

    /// <summary>Run lexicon forbid / prefer rules. Default true for fiction, false for nonfiction unless set.</summary>
    public bool? EnableLexicon { get; init; }

    /// <summary>Run AI-slop pattern rules. Default true.</summary>
    public bool EnableSlop { get; init; } = true;

    /// <summary>Run naming-variant rules. Default true.</summary>
    public bool EnableNaming { get; init; } = true;

    /// <summary>Extra variant→canonical name pairs (merged with built-in Calypso cast table).</summary>
    public IReadOnlyDictionary<string, string>? ExtraNames { get; init; }

    /// <summary>Effective lexicon enablement.</summary>
    public bool LexiconEnabled =>
        EnableLexicon ?? Profile == EditorialProfile.Fiction;
}
