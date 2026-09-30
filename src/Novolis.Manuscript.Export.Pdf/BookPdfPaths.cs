namespace Novolis.Manuscript.Export.Pdf;

/// <summary>PDF paths written by <see cref="BookPdfWriter"/>.</summary>
/// <param name="CombinedPdfPath">Combined book PDF, or null when only chapters were requested.</param>
/// <param name="ChaptersDirectory">Chapter-PDF folder, or null when only the combined file was requested.</param>
/// <param name="ChapterPdfPaths">Per-chapter PDF files inside <paramref name="ChaptersDirectory"/>.</param>
public sealed record BookPdfPaths(
    string? CombinedPdfPath,
    string? ChaptersDirectory,
    IReadOnlyList<string> ChapterPdfPaths);
