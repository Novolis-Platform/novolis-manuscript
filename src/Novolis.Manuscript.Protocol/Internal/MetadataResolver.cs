namespace Novolis.Manuscript.Protocol.Internal;

static class MetadataResolver
{
    public static BookMetadata Resolve(
        BookMetadata book,
        DefaultsMetadata? seriesDefaults,
        DefaultsMetadata? scopeDefaults,
        DefaultsMetadata? workspaceDefaults)
    {
        var authors = book.Authors
            ?? seriesDefaults?.Authors
            ?? scopeDefaults?.Authors
            ?? workspaceDefaults?.Authors;

        var language = book.Language
            ?? seriesDefaults?.Language
            ?? scopeDefaults?.Language
            ?? workspaceDefaults?.Language;

        var rights = book.Rights
            ?? seriesDefaults?.Rights
            ?? scopeDefaults?.Rights
            ?? workspaceDefaults?.Rights;

        return book with
        {
            Authors = authors,
            Language = language,
            Rights = rights,
        };
    }
}
