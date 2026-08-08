namespace Novolis.Manuscript.Protocol;

/// <summary>Stable NMP diagnostic codes.</summary>
public static class ManuscriptDiagnosticCodes
{
    /// <summary>Unsupported protocol major version.</summary>
    public const string UnsupportedProtocolVersion = "NMP001";

    /// <summary>Invalid workspace metadata.</summary>
    public const string InvalidWorkspaceMetadata = "NMP002";

    /// <summary>Invalid authored identifier.</summary>
    public const string InvalidIdentifier = "NMP003";

    /// <summary>Missing universe.yaml.</summary>
    public const string MissingUniverseMetadata = "NMP004";

    /// <summary>Missing subject.yaml.</summary>
    public const string MissingSubjectMetadata = "NMP005";

    /// <summary>Missing series.yaml.</summary>
    public const string MissingSeriesMetadata = "NMP006";

    /// <summary>Missing book.yaml.</summary>
    public const string MissingBookMetadata = "NMP007";

    /// <summary>Missing book title.</summary>
    public const string MissingBookTitle = "NMP008";

    /// <summary>Missing Chapters directory.</summary>
    public const string MissingChaptersDirectory = "NMP009";

    /// <summary>Duplicate document order within a folder.</summary>
    public const string DuplicateDocumentOrder = "NMP010";

    /// <summary>Invalid chapter/appendix filename.</summary>
    public const string InvalidDocumentFilename = "NMP011";

    /// <summary>Missing document H1 title.</summary>
    public const string MissingDocumentTitle = "NMP012";

    /// <summary>YAML parse failure.</summary>
    public const string InvalidYaml = "NMP013";

    /// <summary>Unknown protocol metadata field.</summary>
    public const string UnknownMetadataField = "NMP014";

    /// <summary>Resolved path escapes the workspace.</summary>
    public const string PathEscapesWorkspace = "NMP015";

    /// <summary>Book has no chapters.</summary>
    public const string EmptyBook = "NMP101";

    /// <summary>References folder exists but is empty.</summary>
    public const string EmptyReferenceFolder = "NMP102";

    /// <summary>Series book missing order.</summary>
    public const string SeriesBookMissingOrder = "NMP103";

    /// <summary>Duplicate series book order.</summary>
    public const string DuplicateSeriesOrder = "NMP104";

    /// <summary>Assets folder has files never referenced (best-effort).</summary>
    public const string UnusedAssets = "NMP105";

    /// <summary>book.yaml title differs from chapter H1 (reserved; unused in NMP/1 core).</summary>
    public const string MetadataTitleDiffersFromHeading = "NMP106";
}

/// <summary>One protocol diagnostic finding.</summary>
public sealed record ManuscriptDiagnostic(
    ManuscriptDiagnosticSeverity Severity,
    string Code,
    string Message,
    string Path);
