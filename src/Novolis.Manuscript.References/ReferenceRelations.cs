namespace Novolis.Manuscript.References;

/// <summary>Suggested link relations (open vocabulary — not enforced).</summary>
public static class ReferenceRelations
{
    /// <summary>A document mentions an entry.</summary>
    public const string Mentions = "mentions";

    /// <summary>Entry points at a related entry.</summary>
    public const string SeeAlso = "see-also";

    /// <summary>Entry or document belongs to a set/card container.</summary>
    public const string ContainedIn = "contained-in";
}
