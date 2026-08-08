namespace Novolis.Manuscript;

/// <summary>One chapter prepared for print / speech / markdown export.</summary>
/// <param name="Id">Chapter id (usually filename stem).</param>
/// <param name="Title">Display title from the first H1 (without leading <c>#</c>).</param>
/// <param name="HeadingMarkdown">Full H1 markdown line, e.g. <c># Chapter 1 - Lunch</c>.</param>
/// <param name="PublicFields">Reader-facing metadata rows (tag + value).</param>
/// <param name="ReaderDatelineLines">Public values only (date+time merged when adjacent).</param>
/// <param name="HiddenFields">Authoring fields excluded from reader builds.</param>
/// <param name="BodyMarkdown">Prose after heading/metadata (no front matter, no callouts).</param>
/// <param name="SourcePath">Optional source file path.</param>
/// <param name="Format">Detected metadata format.</param>
public sealed record ChapterPrintView(
    string Id,
    string Title,
    string HeadingMarkdown,
    IReadOnlyList<(string Tag, string Value)> PublicFields,
    IReadOnlyList<string> ReaderDatelineLines,
    IReadOnlyDictionary<string, string> HiddenFields,
    string BodyMarkdown,
    string? SourcePath,
    ManuscriptMetadataFormat Format);

/// <summary>Cover / book-level fields for print assemblies.</summary>
public sealed record BookPrintCover(
    string Title,
    string? Subtitle,
    string? Series,
    string? Author,
    string? Rights);

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
