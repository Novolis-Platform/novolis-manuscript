namespace Novolis.Manuscript.Protocol;

/// <summary>Immutable workspace catalog.</summary>
public sealed record ManuscriptCatalog(
    IReadOnlyList<FictionUniverse> Fiction,
    IReadOnlyList<NonFictionSubject> NonFiction);
