namespace Novolis.Manuscript.Protocol.Internal;

static class ProtocolValidator
{
    public static void Validate(ManuscriptCatalog catalog, List<ManuscriptDiagnostic> diagnostics)
    {
        foreach (var universe in catalog.Fiction)
        {
            foreach (var series in universe.Series)
            {
                ValidateSeriesOrders(series, diagnostics);
                foreach (var book in series.Books)
                    ValidateBook(book, diagnostics);
            }

            foreach (var book in universe.Books)
                ValidateBook(book, diagnostics);
        }

        foreach (var subject in catalog.NonFiction)
        {
            foreach (var book in subject.Books)
                ValidateBook(book, diagnostics);
        }
    }

    static void ValidateSeriesOrders(ManuscriptSeries series, List<ManuscriptDiagnostic> diagnostics)
    {
        var seen = new Dictionary<int, string>();
        foreach (var book in series.Books)
        {
            if (book.Metadata.Order is null)
            {
                diagnostics.Add(new ManuscriptDiagnostic(
                    ManuscriptDiagnosticSeverity.Warning,
                    ManuscriptDiagnosticCodes.SeriesBookMissingOrder,
                    $"Series book '{book.Address.BookId}' is missing order.",
                    book.Address.BookId));
                continue;
            }

            var order = book.Metadata.Order.Value;
            if (seen.TryGetValue(order, out var prior))
            {
                diagnostics.Add(new ManuscriptDiagnostic(
                    ManuscriptDiagnosticSeverity.Warning,
                    ManuscriptDiagnosticCodes.DuplicateSeriesOrder,
                    $"Duplicate series order {order} for '{book.Address.BookId}' (also '{prior}').",
                    book.Address.BookId));
            }
            else
            {
                seen[order] = book.Address.BookId;
            }
        }
    }

    static void ValidateBook(ManuscriptBook book, List<ManuscriptDiagnostic> diagnostics)
    {
        if (book.Chapters.Count == 0)
        {
            diagnostics.Add(new ManuscriptDiagnostic(
                ManuscriptDiagnosticSeverity.Warning,
                ManuscriptDiagnosticCodes.EmptyBook,
                $"Book '{book.Address.BookId}' has no chapters.",
                book.Address.BookId));
        }
    }
}
