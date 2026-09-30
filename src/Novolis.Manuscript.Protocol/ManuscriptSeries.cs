namespace Novolis.Manuscript.Protocol;

/// <summary>Fiction series node.</summary>
public sealed record ManuscriptSeries(
    string Id,
    SeriesMetadata Metadata,
    IReadOnlyList<ManuscriptBook> Books,
    IReadOnlyList<ReferenceDocument> References);
