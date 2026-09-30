namespace Novolis.Manuscript.Protocol;

/// <summary>Fiction universe node.</summary>
public sealed record FictionUniverse(
    string Id,
    UniverseMetadata Metadata,
    IReadOnlyList<ManuscriptSeries> Series,
    IReadOnlyList<ManuscriptBook> Books,
    IReadOnlyList<ReferenceDocument> References);
