namespace Novolis.Manuscript.Editorial;

/// <summary>Optional content packs for editorial rules (apps opt in).</summary>
public static class EditorialProfiles
{
    /// <summary>Calypso / Galactic Confederation cast spelling variants.</summary>
    public static readonly IReadOnlyDictionary<string, string> CalypsoNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Marshe"] = "Marsh",
            ["Marsch"] = "Marsh",
            ["Rynn"] = "Ryn",
            ["Rin"] = "Ryn",
            ["Mirra"] = "Mira",
            ["Myra"] = "Mira",
            ["Kethera"] = "Kethra",
            ["Kethera Sel"] = "Kethra Sel",
            ["Ixah"] = "Ixa",
            ["James Simmon"] = "James Simmons",
        };

    /// <summary>Calypso forbid-list (wrong-universe / wrong-tech).</summary>
    public static readonly IReadOnlyList<string> CalypsoForbiddenPhrases =
    [
        "warp drive", "warp core", "warp factor", "warp bubble",
        "hyperdrive core", "hyperspace lane", "photon torpedo", "photon torpedoes",
        "quantum torpedo", "quantum torpedoes", "quantum drive", "quantum leap",
        "beam up", "beam out", "dilithium chamber", "impulse drive", "deflector dish",
        "tractor beam", "ludicrous speed", "lightspeed drive", "AI singularity",
        "laser pistol", "ray gun",
        "warp", "hyperspace", "hyperspeed", "hyperdrive", "subspace",
        "phaser", "phasers", "transporter", "dilithium", "replicator", "holodeck",
        "Starfleet", "Federation", "blaster", "hypersleep",
    ];

    /// <summary>Calypso ship/station prefer pairs.</summary>
    public static readonly IReadOnlyList<(string Flagged, string Prefer)> CalypsoPreferPairs =
    [
        ("hallway", "corridor"),
        ("ceiling", "overhead"),
    ];

    /// <summary>Neutral fiction defaults (no content-specific lexicon or names).</summary>
    public static EditorialOptions FictionNeutral() => new()
    {
        Profile = EditorialProfile.Fiction,
        EnableLexicon = false,
        EnableSlop = true,
        EnableNaming = true,
    };

    /// <summary>Calypso-oriented pack: lexicon + cast names.</summary>
    public static EditorialOptions Calypso() => new()
    {
        Profile = EditorialProfile.Calypso,
        EnableLexicon = true,
        EnableSlop = true,
        EnableNaming = true,
        ExtraNames = CalypsoNames,
        ForbiddenPhrases = CalypsoForbiddenPhrases,
        PreferPairs = CalypsoPreferPairs,
    };

    /// <summary>Non-fiction defaults.</summary>
    public static EditorialOptions Nonfiction() => new()
    {
        Profile = EditorialProfile.Nonfiction,
        EnableLexicon = false,
        EnableSlop = true,
        EnableNaming = true,
    };
}
