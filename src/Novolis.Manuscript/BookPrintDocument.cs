namespace Novolis.Manuscript;

/// <summary>Ordered print model for a whole book.</summary>
/// <param name="BookId">Output stem / book id.</param>
/// <param name="Cover">Cover metadata.</param>
/// <param name="Chapters">Ordered chapter views.</param>
/// <param name="DebugMode">When true, author-style hidden fields may be shown by exporters.</param>
public sealed record BookPrintDocument(
    string BookId,
    BookPrintCover Cover,
    IReadOnlyList<ChapterPrintView> Chapters,
    bool DebugMode);
