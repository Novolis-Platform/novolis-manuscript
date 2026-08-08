namespace Novolis.Manuscript.Protocol.Internal;

sealed class CatalogReader
{
    readonly ProtocolMetadataReader _metadata = new();
    readonly DocumentReader _documents;
    readonly ReferenceReader _references;

    public CatalogReader()
    {
        _documents = new DocumentReader(_metadata);
        _references = new ReferenceReader(_metadata);
    }

    public ManuscriptSnapshot Read(string workspaceRoot, WorkspaceMetadata workspace)
    {
        var diagnostics = new List<ManuscriptDiagnostic>();
        var fiction = new List<FictionUniverse>();
        var nonFiction = new List<NonFictionSubject>();

        var src = Path.Combine(workspaceRoot, ProtocolNames.Src);
        if (Directory.Exists(src))
        {
            var fictionRoot = Path.Combine(src, ProtocolNames.Fiction);
            if (Directory.Exists(fictionRoot))
            {
                foreach (var universeDir in EnumerateAuthoredDirs(fictionRoot))
                {
                    var universe = ReadUniverse(universeDir, workspace, workspaceRoot, diagnostics);
                    if (universe is not null)
                        fiction.Add(universe);
                }
            }

            var nonFictionRoot = Path.Combine(src, ProtocolNames.NonFiction);
            if (Directory.Exists(nonFictionRoot))
            {
                foreach (var subjectDir in EnumerateAuthoredDirs(nonFictionRoot))
                {
                    var subject = ReadSubject(subjectDir, workspace, workspaceRoot, diagnostics);
                    if (subject is not null)
                        nonFiction.Add(subject);
                }
            }
        }

        var catalog = new ManuscriptCatalog(fiction, nonFiction);
        ProtocolValidator.Validate(catalog, diagnostics);
        return new ManuscriptSnapshot(catalog, diagnostics);
    }

    FictionUniverse? ReadUniverse(
        string universeDir,
        WorkspaceMetadata workspace,
        string workspaceRoot,
        List<ManuscriptDiagnostic> diagnostics)
    {
        var id = Path.GetFileName(universeDir);
        if (!IdentifierRules.IsValidId(id))
        {
            diagnostics.Add(InvalidId(id, universeDir));
            return null;
        }

        var marker = Path.Combine(universeDir, ProtocolNames.UniverseMarker);
        if (!File.Exists(marker))
        {
            diagnostics.Add(new ManuscriptDiagnostic(
                ManuscriptDiagnosticSeverity.Error,
                ManuscriptDiagnosticCodes.MissingUniverseMetadata,
                "Missing universe.yaml.",
                universeDir));
            return null;
        }

        var metaResult = _metadata.ReadUniverse(marker, diagnostics);
        if (!metaResult.Success)
            return null;

        var series = new List<ManuscriptSeries>();
        var books = new List<ManuscriptBook>();

        foreach (var child in EnumerateAuthoredDirs(universeDir))
        {
            var seriesMarker = Path.Combine(child, ProtocolNames.SeriesMarker);
            var bookMarker = Path.Combine(child, ProtocolNames.BookMarker);
            if (File.Exists(seriesMarker))
            {
                var s = ReadSeries(child, id, metaResult.Value!, workspace, workspaceRoot, diagnostics);
                if (s is not null)
                    series.Add(s);
            }
            else if (File.Exists(bookMarker))
            {
                var b = ReadBook(
                    child,
                    new ManuscriptAddress(ManuscriptKind.Fiction, id, null, Path.GetFileName(child)),
                    seriesDefaults: null,
                    scopeDefaults: metaResult.Value!.Defaults,
                    workspace.Defaults,
                    workspaceRoot,
                    diagnostics);
                if (b is not null)
                    books.Add(b);
            }
        }

        var refs = _references.Read(
            Path.Combine(universeDir, ProtocolNames.References),
            $"fiction/{id}/reference",
            workspaceRoot,
            diagnostics);

        return new FictionUniverse(id, metaResult.Value!, series, books, refs);
    }

    NonFictionSubject? ReadSubject(
        string subjectDir,
        WorkspaceMetadata workspace,
        string workspaceRoot,
        List<ManuscriptDiagnostic> diagnostics)
    {
        var id = Path.GetFileName(subjectDir);
        if (!IdentifierRules.IsValidId(id))
        {
            diagnostics.Add(InvalidId(id, subjectDir));
            return null;
        }

        var marker = Path.Combine(subjectDir, ProtocolNames.SubjectMarker);
        if (!File.Exists(marker))
        {
            diagnostics.Add(new ManuscriptDiagnostic(
                ManuscriptDiagnosticSeverity.Error,
                ManuscriptDiagnosticCodes.MissingSubjectMetadata,
                "Missing subject.yaml.",
                subjectDir));
            return null;
        }

        var metaResult = _metadata.ReadSubject(marker, diagnostics);
        if (!metaResult.Success)
            return null;

        var books = new List<ManuscriptBook>();
        foreach (var child in EnumerateAuthoredDirs(subjectDir))
        {
            var bookMarker = Path.Combine(child, ProtocolNames.BookMarker);
            if (!File.Exists(bookMarker))
                continue;

            var b = ReadBook(
                child,
                new ManuscriptAddress(ManuscriptKind.NonFiction, id, null, Path.GetFileName(child)),
                seriesDefaults: null,
                scopeDefaults: metaResult.Value!.Defaults,
                workspace.Defaults,
                workspaceRoot,
                diagnostics);
            if (b is not null)
                books.Add(b);
        }

        var refs = _references.Read(
            Path.Combine(subjectDir, ProtocolNames.References),
            $"nonfiction/{id}/reference",
            workspaceRoot,
            diagnostics);

        return new NonFictionSubject(id, metaResult.Value!, books, refs);
    }

    ManuscriptSeries? ReadSeries(
        string seriesDir,
        string universeId,
        UniverseMetadata universe,
        WorkspaceMetadata workspace,
        string workspaceRoot,
        List<ManuscriptDiagnostic> diagnostics)
    {
        var id = Path.GetFileName(seriesDir);
        if (!IdentifierRules.IsValidId(id))
        {
            diagnostics.Add(InvalidId(id, seriesDir));
            return null;
        }

        var marker = Path.Combine(seriesDir, ProtocolNames.SeriesMarker);
        if (!File.Exists(marker))
        {
            diagnostics.Add(new ManuscriptDiagnostic(
                ManuscriptDiagnosticSeverity.Error,
                ManuscriptDiagnosticCodes.MissingSeriesMetadata,
                "Missing series.yaml.",
                seriesDir));
            return null;
        }

        var metaResult = _metadata.ReadSeries(marker, diagnostics);
        if (!metaResult.Success)
            return null;

        var books = new List<ManuscriptBook>();
        foreach (var child in EnumerateAuthoredDirs(seriesDir))
        {
            var bookMarker = Path.Combine(child, ProtocolNames.BookMarker);
            if (!File.Exists(bookMarker))
                continue;

            var b = ReadBook(
                child,
                new ManuscriptAddress(ManuscriptKind.Fiction, universeId, id, Path.GetFileName(child)),
                seriesDefaults: metaResult.Value!.Defaults,
                scopeDefaults: universe.Defaults,
                workspace.Defaults,
                workspaceRoot,
                diagnostics);
            if (b is not null)
                books.Add(b);
        }

        var refs = _references.Read(
            Path.Combine(seriesDir, ProtocolNames.References),
            $"fiction/{universeId}/{id}/reference",
            workspaceRoot,
            diagnostics);

        return new ManuscriptSeries(id, metaResult.Value!, books, refs);
    }

    ManuscriptBook? ReadBook(
        string bookDir,
        ManuscriptAddress address,
        DefaultsMetadata? seriesDefaults,
        DefaultsMetadata? scopeDefaults,
        DefaultsMetadata? workspaceDefaults,
        string workspaceRoot,
        List<ManuscriptDiagnostic> diagnostics)
    {
        if (!IdentifierRules.IsValidId(address.BookId))
        {
            diagnostics.Add(InvalidId(address.BookId, bookDir));
            return null;
        }

        var marker = Path.Combine(bookDir, ProtocolNames.BookMarker);
        if (!File.Exists(marker))
        {
            diagnostics.Add(new ManuscriptDiagnostic(
                ManuscriptDiagnosticSeverity.Error,
                ManuscriptDiagnosticCodes.MissingBookMetadata,
                "Missing book.yaml.",
                bookDir));
            return null;
        }

        var metaResult = _metadata.ReadBook(marker, diagnostics);
        if (!metaResult.Success)
            return null;

        var chaptersDir = Path.Combine(bookDir, ProtocolNames.Chapters);
        if (!Directory.Exists(chaptersDir))
        {
            diagnostics.Add(new ManuscriptDiagnostic(
                ManuscriptDiagnosticSeverity.Error,
                ManuscriptDiagnosticCodes.MissingChaptersDirectory,
                "Missing Chapters directory.",
                bookDir));
        }

        var chapters = _documents.ReadDocuments(chaptersDir, ManuscriptDocumentKind.Chapter, diagnostics);
        var appendices = _documents.ReadDocuments(
            Path.Combine(bookDir, ProtocolNames.Appendices),
            ManuscriptDocumentKind.Appendix,
            diagnostics);

        var effective = MetadataResolver.Resolve(metaResult.Value!, seriesDefaults, scopeDefaults, workspaceDefaults);

        var refs = _references.Read(
            Path.Combine(bookDir, ProtocolNames.References),
            address.SeriesId is null
                ? address.Kind == ManuscriptKind.Fiction
                    ? $"fiction/{address.ScopeId}/{address.BookId}/reference"
                    : $"nonfiction/{address.ScopeId}/{address.BookId}/reference"
                : $"fiction/{address.ScopeId}/{address.SeriesId}/{address.BookId}/reference",
            workspaceRoot,
            diagnostics);

        return new ManuscriptBook(address, effective, chapters, appendices, refs);
    }

    static IEnumerable<string> EnumerateAuthoredDirs(string parent)
    {
        if (!Directory.Exists(parent))
            yield break;

        foreach (var dir in Directory.GetDirectories(parent).OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(dir);
            if (IdentifierRules.IsHiddenName(name))
                continue;
            if (ProtocolNames.ReservedDirectoryNames.Contains(name))
                continue;
            yield return dir;
        }
    }

    static ManuscriptDiagnostic InvalidId(string id, string path) =>
        new(ManuscriptDiagnosticSeverity.Error,
            ManuscriptDiagnosticCodes.InvalidIdentifier,
            $"Invalid identifier '{id}'. Use lowercase kebab-case and avoid reserved names.",
            path);
}
