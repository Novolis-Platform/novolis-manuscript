namespace Novolis.Manuscript.Protocol;

/// <summary>Catalog plus diagnostics from a single read.</summary>
public sealed record ManuscriptSnapshot(
    ManuscriptCatalog Catalog,
    IReadOnlyList<ManuscriptDiagnostic> Diagnostics);
