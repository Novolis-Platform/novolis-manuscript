namespace Novolis.Manuscript.Editorial;

/// <summary>Editorial rule profile.</summary>
public enum EditorialProfile
{
    /// <summary>Generic fiction — no content-specific lexicon/names unless supplied.</summary>
    Fiction = 0,

    /// <summary>Non-fiction / textbook — lexicon off by default; slop stems still apply.</summary>
    Nonfiction = 1,

    /// <summary>Calypso / GC fiction content pack (lexicon + cast names).</summary>
    Calypso = 2,
}
