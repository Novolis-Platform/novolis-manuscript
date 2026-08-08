namespace Novolis.Manuscript.Protocol.Internal;

static class ProtocolNames
{
    public const string WorkspaceMarker = "manuscript.yaml";
    public const string UniverseMarker = "universe.yaml";
    public const string SubjectMarker = "subject.yaml";
    public const string SeriesMarker = "series.yaml";
    public const string BookMarker = "book.yaml";

    public const string Src = "src";
    public const string Fiction = "Fiction";
    public const string NonFiction = "NonFiction";
    public const string Chapters = "Chapters";
    public const string Appendices = "Appendices";
    public const string References = "References";
    public const string Assets = "Assets";

    public const string ProtocolId = "novolis.manuscript";
    public const int SupportedMajorVersion = 1;

    public static readonly HashSet<string> ReservedDirectoryNames = new(StringComparer.Ordinal)
    {
        Src, Fiction, NonFiction, Chapters, Appendices, References, Assets,
    };

    public static readonly HashSet<string> WorkspaceKeys = new(StringComparer.Ordinal)
    {
        "protocol", "version", "defaults", "extensions",
    };

    public static readonly HashSet<string> EntityKeys = new(StringComparer.Ordinal)
    {
        "title", "description", "defaults", "extensions",
    };

    public static readonly HashSet<string> BookKeys = new(StringComparer.Ordinal)
    {
        "title", "subtitle", "order", "authors", "language", "description", "rights",
        "targets", "publication", "extensions",
    };

    public static readonly HashSet<string> DefaultsKeys = new(StringComparer.Ordinal)
    {
        "authors", "language", "rights",
    };

    public static readonly HashSet<string> TargetsKeys = new(StringComparer.Ordinal)
    {
        "words",
    };

    public static readonly HashSet<string> PublicationKeys = new(StringComparer.Ordinal)
    {
        "version", "isbn", "date",
    };

    public static readonly HashSet<string> ChapterKeys = new(StringComparer.Ordinal)
    {
        "status", "tags", "date", "time", "system", "locations", "pov", "characters", "extensions",
    };

    public static readonly HashSet<string> ReferenceKeys = new(StringComparer.Ordinal)
    {
        "aliases", "tags", "extensions",
    };
}
