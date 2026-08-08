namespace Novolis.Manuscript.Editorial;

/// <summary>Editorial rule profile.</summary>
public enum EditorialProfile
{
    /// <summary>Fiction manuscript rules (Calypso-oriented lexicon defaults).</summary>
    Fiction = 0,

    /// <summary>Non-fiction / textbook — lexicon forbid list off by default; slop stems still apply.</summary>
    Nonfiction = 1,
}
