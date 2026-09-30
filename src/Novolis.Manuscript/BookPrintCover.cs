namespace Novolis.Manuscript;

/// <summary>Cover / book-level fields for print assemblies.</summary>
public sealed record BookPrintCover(
    string Title,
    string? Subtitle,
    string? Series,
    string? Author,
    string? Rights);
