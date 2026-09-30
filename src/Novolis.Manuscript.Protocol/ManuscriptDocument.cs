namespace Novolis.Manuscript.Protocol;

/// <summary>Ordered chapter or appendix document.</summary>
public sealed record ManuscriptDocument(
    string Slug,
    int Order,
    string Title,
    ManuscriptDocumentKind Kind,
    string FilePath,
    ChapterMetadata Metadata);
