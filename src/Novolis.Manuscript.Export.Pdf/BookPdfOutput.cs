namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Which PDF files a book print writes.</summary>
public enum BookPdfOutput
{
    /// <summary>One combined PDF at <c>{stem}.pdf</c> (default).</summary>
    Combine,

    /// <summary>Per-chapter PDFs under <c>{stem}-chapters/</c> only.</summary>
    Chapters,

    /// <summary>Combined PDF and the chapter folder.</summary>
    Both,
}
