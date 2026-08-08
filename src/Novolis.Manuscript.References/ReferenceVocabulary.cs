namespace Novolis.Manuscript.References;

/// <summary>Suggested facet keys for reference cards (open vocabulary — not enforced).</summary>
public static class ReferenceFacets
{
    /// <summary>Author-defined kind (ship, person, place, …).</summary>
    public const string Kind = "kind";

    /// <summary>Catalog scope (series, book, set).</summary>
    public const string Scope = "scope";

    /// <summary>Reference set id from the catalog.</summary>
    public const string Set = "set";

    /// <summary>Series id when known.</summary>
    public const string Series = "series";

    /// <summary>Book id when known.</summary>
    public const string Book = "book";
}

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
