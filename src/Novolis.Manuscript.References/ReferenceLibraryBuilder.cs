using Novolis.IO.Indexing;

namespace Novolis.Manuscript.References;

/// <summary>Builds a <see cref="ReferenceLibrary"/> on top of <see cref="ContentIndexBuilder"/>.</summary>
public sealed class ReferenceLibraryBuilder
{
    readonly ContentIndexBuilder _index = new();

    /// <summary>Underlying index builder for advanced hosts.</summary>
    public ContentIndexBuilder Index => _index;

    /// <summary>Registers a reference card (entry + optional backing document).</summary>
    public ReferenceLibraryBuilder AddCard(
        string id,
        string? title = null,
        string? location = null,
        IEnumerable<string>? aliases = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? facets = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!string.IsNullOrWhiteSpace(location))
            _index.AddDocument(id, location);
        _index.AddEntry(id, title, aliases, facets);
        return this;
    }

    /// <summary>
    /// Registers catalog reference files as cards without reading file contents
    /// (id, title, path only — no Markdown or YAML parsing).
    /// </summary>
    public ReferenceLibraryBuilder AddReferenceSet(
        ReferenceSetInfo set,
        string? seriesId = null,
        string? bookId = null)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentException.ThrowIfNullOrWhiteSpace(set.Id);

        _index.AddEntry(set.Id, set.Title, facets: Facets(
            (ReferenceFacets.Scope, "set"),
            (ReferenceFacets.Series, seriesId),
            (ReferenceFacets.Book, bookId)));

        if (!string.IsNullOrWhiteSpace(set.DirectoryPath))
            _index.AddDocument(set.Id, set.DirectoryPath);

        foreach (var file in set.Files)
        {
            var facets = Facets(
                (ReferenceFacets.Scope, "file"),
                (ReferenceFacets.Set, set.Id),
                (ReferenceFacets.Series, seriesId),
                (ReferenceFacets.Book, bookId));

            AddCard(file.Id, file.Title, file.FilePath, facets: facets);
            _index.AddLink(
                new IndexEndpoint(IndexEndpointKind.Entry, file.Id),
                new IndexEndpoint(IndexEndpointKind.Entry, set.Id),
                relation: ReferenceRelations.ContainedIn);
        }

        return this;
    }

    /// <summary>Registers every reference set on a series (catalog metadata only).</summary>
    public ReferenceLibraryBuilder AddSeries(SeriesInfo series)
    {
        ArgumentNullException.ThrowIfNull(series);
        foreach (var set in series.References)
            AddReferenceSet(set, seriesId: series.Id);
        return this;
    }

    /// <summary>Registers every reference set on a book (catalog metadata only).</summary>
    public ReferenceLibraryBuilder AddBook(BookInfo book)
    {
        ArgumentNullException.ThrowIfNull(book);
        foreach (var set in book.References)
            AddReferenceSet(set, seriesId: book.SeriesId, bookId: book.Id);
        return this;
    }

    /// <summary>Records that a document mentions an entry (by id or alias; target need not exist yet).</summary>
    public ReferenceLibraryBuilder Mention(
        string documentId,
        string entryIdOrAlias,
        IndexSpan? span = null,
        string? relation = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryIdOrAlias);
        _index.AddDocument(documentId);
        _index.AddLink(
            new IndexEndpoint(IndexEndpointKind.Document, documentId),
            new IndexEndpoint(IndexEndpointKind.Entry, entryIdOrAlias),
            relation: relation ?? ReferenceRelations.Mentions,
            provenanceDocumentId: documentId,
            span: span);
        return this;
    }

    /// <summary>See-also link between two entries.</summary>
    public ReferenceLibraryBuilder SeeAlso(string fromEntryId, string toEntryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromEntryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toEntryId);
        _index.AddLink(
            new IndexEndpoint(IndexEndpointKind.Entry, fromEntryId),
            new IndexEndpoint(IndexEndpointKind.Entry, toEntryId),
            relation: ReferenceRelations.SeeAlso);
        return this;
    }

    /// <summary>Builds an immutable library snapshot.</summary>
    public ReferenceLibrary Build() => new(_index.Build());

    static Dictionary<string, IReadOnlyList<string>> Facets(params (string Key, string? Value)[] pairs)
    {
        var map = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in pairs)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;
            map[key] = [value];
        }

        return map;
    }
}
