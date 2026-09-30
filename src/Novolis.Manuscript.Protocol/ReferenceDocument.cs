namespace Novolis.Manuscript.Protocol;

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
