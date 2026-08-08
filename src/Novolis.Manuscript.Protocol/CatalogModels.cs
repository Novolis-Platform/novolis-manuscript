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

/// <summary>Immutable workspace catalog.</summary>
public sealed record ManuscriptCatalog(
    IReadOnlyList<FictionUniverse> Fiction,
    IReadOnlyList<NonFictionSubject> NonFiction);

/// <summary>Fiction universe node.</summary>
public sealed record FictionUniverse(
    string Id,
    UniverseMetadata Metadata,
    IReadOnlyList<ManuscriptSeries> Series,
    IReadOnlyList<ManuscriptBook> Books,
    IReadOnlyList<ReferenceDocument> References);

/// <summary>Non-fiction subject node.</summary>
public sealed record NonFictionSubject(
    string Id,
    SubjectMetadata Metadata,
    IReadOnlyList<ManuscriptBook> Books,
    IReadOnlyList<ReferenceDocument> References);

/// <summary>Fiction series node.</summary>
public sealed record ManuscriptSeries(
    string Id,
    SeriesMetadata Metadata,
    IReadOnlyList<ManuscriptBook> Books,
    IReadOnlyList<ReferenceDocument> References);

/// <summary>Book node with chapters, appendices, and book-scoped references.</summary>
public sealed record ManuscriptBook(
    ManuscriptAddress Address,
    BookMetadata Metadata,
    IReadOnlyList<ManuscriptDocument> Chapters,
    IReadOnlyList<ManuscriptDocument> Appendices,
    IReadOnlyList<ReferenceDocument> References);

/// <summary>Ordered chapter or appendix document.</summary>
public sealed record ManuscriptDocument(
    string Slug,
    int Order,
    string Title,
    ManuscriptDocumentKind Kind,
    string FilePath,
    ChapterMetadata Metadata);

/// <summary>Reference markdown document with scoped catalog identity.</summary>
/// <param name="Id">Scoped identity (e.g. <c>fiction/…/reference/…</c>).</param>
/// <param name="Title">First H1, or file stem.</param>
/// <param name="RelativePath">Path relative to the References root.</param>
/// <param name="FilePath">Absolute filesystem path.</param>
/// <param name="Metadata">Optional front matter.</param>
public sealed record ReferenceDocument(
    string Id,
    string Title,
    string RelativePath,
    string FilePath,
    ReferenceMetadata Metadata);

/// <summary>Catalog plus diagnostics from a single read.</summary>
public sealed record ManuscriptSnapshot(
    ManuscriptCatalog Catalog,
    IReadOnlyList<ManuscriptDiagnostic> Diagnostics);
