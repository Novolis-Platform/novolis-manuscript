namespace Novolis.Manuscript.Protocol;

/// <summary>Non-fiction subject node.</summary>
public sealed record NonFictionSubject(
    string Id,
    SubjectMetadata Metadata,
    IReadOnlyList<ManuscriptBook> Books,
    IReadOnlyList<ReferenceDocument> References);
