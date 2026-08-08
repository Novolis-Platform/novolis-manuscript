using Novolis.IO.Indexing;

namespace Novolis.Manuscript.References;

/// <summary>Domain façade over an in-memory <see cref="ContentIndex"/> for manuscript references.</summary>
public sealed class ReferenceLibrary
{
    /// <summary>Creates a library around an existing index snapshot.</summary>
    public ReferenceLibrary(ContentIndex index)
    {
        Index = index ?? throw new ArgumentNullException(nameof(index));
    }

    /// <summary>Underlying format-agnostic index.</summary>
    public ContentIndex Index { get; }

    /// <summary>Resolves a card by id or alias.</summary>
    public bool TryResolve(string idOrAlias, out IndexEntry card) =>
        Index.TryResolveEntry(idOrAlias, out card);

    /// <summary>Cards with a given facet value.</summary>
    public IEnumerable<IndexEntry> CardsByFacet(string key, string value) =>
        Index.FindByFacet(key, value);

    /// <summary>Cards in a catalog reference set.</summary>
    public IEnumerable<IndexEntry> CardsInSet(string setId) =>
        CardsByFacet(ReferenceFacets.Set, setId);

    /// <summary>Mention links from a document.</summary>
    public IEnumerable<IndexLink> MentionsFrom(string documentId) =>
        Index.GetLinksFrom(new IndexEndpoint(IndexEndpointKind.Document, documentId))
            .Where(l => string.Equals(l.Relation, ReferenceRelations.Mentions, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Mention targets that do not resolve to a registered entry
    /// (soft links / author debt).
    /// </summary>
    public IEnumerable<string> UnresolvedMentionTargets()
    {
        foreach (var link in Index.GetLinksByRelation(ReferenceRelations.Mentions))
        {
            if (link.To.Kind != IndexEndpointKind.Entry)
                continue;
            if (!Index.TryResolveEntry(link.To.Id, out _))
                yield return link.To.Id;
        }
    }
}
