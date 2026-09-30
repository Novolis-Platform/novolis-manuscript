namespace Novolis.Manuscript.Protocol;

/// <summary>Structural identity of a book.</summary>
/// <param name="Kind">Fiction or non-fiction.</param>
/// <param name="ScopeId">Universe id (fiction) or subject id (non-fiction).</param>
/// <param name="SeriesId">Series id, or null for standalone books.</param>
/// <param name="BookId">Book directory id.</param>
public sealed record ManuscriptAddress(
    ManuscriptKind Kind,
    string ScopeId,
    string? SeriesId,
    string BookId);
