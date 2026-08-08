using Novolis.Manuscript.Protocol.Internal;

namespace Novolis.Manuscript.Protocol;

/// <summary>An opened NMP/1 manuscript workspace.</summary>
public sealed class ManuscriptWorkspace
{
    readonly CatalogReader _catalog = new();

    ManuscriptWorkspace(string rootPath, WorkspaceMetadata metadata)
    {
        RootPath = rootPath;
        Metadata = metadata;
    }

    /// <summary>Absolute workspace root containing <c>manuscript.yaml</c>.</summary>
    public string RootPath { get; }

    /// <summary>Parsed workspace marker metadata.</summary>
    public WorkspaceMetadata Metadata { get; }

    /// <summary>
    /// Opens a workspace by walking upward for <c>manuscript.yaml</c>, then validating protocol/version.
    /// </summary>
    /// <param name="startPath">File or directory path inside the workspace.</param>
    /// <exception cref="ArgumentException">When <paramref name="startPath"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">When the marker is missing or protocol version is unsupported.</exception>
    /// <exception cref="DirectoryNotFoundException">When the resolved root is inaccessible.</exception>
    public static ManuscriptWorkspace Open(string startPath)
    {
        var root = WorkspaceLocator.LocateRoot(startPath);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Workspace root not found: {root}");

        var marker = Path.Combine(root, ProtocolNames.WorkspaceMarker);
        var diagnostics = new List<ManuscriptDiagnostic>();
        var reader = new ProtocolMetadataReader();
        var result = reader.ReadWorkspace(marker, diagnostics);
        if (!result.Success || result.Value is null)
        {
            var first = diagnostics.FirstOrDefault();
            if (first is not null
                && first.Code == ManuscriptDiagnosticCodes.UnsupportedProtocolVersion)
            {
                throw new InvalidOperationException(first.Message);
            }

            var message = first?.Message ?? "Invalid workspace metadata.";
            throw new InvalidOperationException(message);
        }

        return new ManuscriptWorkspace(root, result.Value);
    }

    /// <summary>Enumerates the canonical tree and returns an immutable catalog with diagnostics.</summary>
    public ManuscriptSnapshot Read() => _catalog.Read(RootPath, Metadata);
}
