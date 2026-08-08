namespace Novolis.Manuscript.Protocol.Internal;

sealed class ReferenceReader(ProtocolMetadataReader metadataReader)
{
    public IReadOnlyList<ReferenceDocument> Read(
        string referencesDirectory,
        string scopeIdPrefix,
        string workspaceRoot,
        List<ManuscriptDiagnostic> diagnostics)
    {
        if (!Directory.Exists(referencesDirectory))
            return [];

        var files = new List<string>();
        CollectMarkdown(referencesDirectory, files);

        if (files.Count == 0)
        {
            diagnostics.Add(new ManuscriptDiagnostic(
                ManuscriptDiagnosticSeverity.Warning,
                ManuscriptDiagnosticCodes.EmptyReferenceFolder,
                "References folder is empty.",
                referencesDirectory));
            return [];
        }

        var results = new List<ReferenceDocument>();
        foreach (var file in files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var full = Path.GetFullPath(file);
            if (!IsUnderRoot(full, workspaceRoot))
            {
                diagnostics.Add(new ManuscriptDiagnostic(
                    ManuscriptDiagnosticSeverity.Error,
                    ManuscriptDiagnosticCodes.PathEscapesWorkspace,
                    "Reference path escapes workspace.",
                    full));
                continue;
            }

            var relative = Path.GetRelativePath(referencesDirectory, full).Replace('\\', '/');
            if (relative.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                relative = relative[..^3];

            var id = $"{scopeIdPrefix}/{relative}";
            var text = File.ReadAllText(full);
            if (text.StartsWith('\uFEFF'))
                text = text[1..];

            var (frontMatter, body) = DocumentReader.SplitFrontMatter(text);
            var metaResult = metadataReader.ReadReferenceFrontMatter(frontMatter, full, diagnostics);
            var metadata = metaResult.Success ? metaResult.Value! : new ReferenceMetadata();
            var title = DocumentReader.ReadFirstH1(body) ?? Path.GetFileNameWithoutExtension(full);

            results.Add(new ReferenceDocument(id, title.Trim(), relative, full, metadata));
        }

        return results;
    }

    static void CollectMarkdown(string directory, List<string> files)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            var name = Path.GetFileName(entry);
            if (IdentifierRules.IsHiddenName(name))
                continue;

            if (Directory.Exists(entry))
            {
                CollectMarkdown(entry, files);
                continue;
            }

            if (File.Exists(entry) && entry.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                files.Add(entry);
        }
    }

    static bool IsUnderRoot(string fullPath, string root)
    {
        var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(fullPath);
        return candidate.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)
               || string.Equals(Path.GetFullPath(root), candidate, StringComparison.OrdinalIgnoreCase);
    }
}
