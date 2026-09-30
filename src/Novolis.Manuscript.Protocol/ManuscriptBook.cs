namespace Novolis.Manuscript.Protocol;

/// <summary>Book node with chapters, appendices, and book-scoped references.</summary>
public sealed record ManuscriptBook(
    ManuscriptAddress Address,
    BookMetadata Metadata,
    IReadOnlyList<ManuscriptDocument> Chapters,
    IReadOnlyList<ManuscriptDocument> Appendices,
    IReadOnlyList<ReferenceDocument> References);
