using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Novolis.Manuscript.Protocol.Internal;

sealed class ProtocolMetadataReader
{
    readonly IDeserializer _deserializer =
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    public Result<WorkspaceMetadata> ReadWorkspace(string path, List<ManuscriptDiagnostic> diagnostics)
    {
        if (!TryLoadMapping(path, diagnostics, out var root, out var raw))
            return Result<WorkspaceMetadata>.Fail();

        ValidateKeys(path, root, ProtocolNames.WorkspaceKeys, diagnostics);
        try
        {
            var dto = _deserializer.Deserialize<WorkspaceYamlDto>(raw) ?? new WorkspaceYamlDto();
            if (!string.Equals(dto.Protocol, ProtocolNames.ProtocolId, StringComparison.Ordinal))
            {
                diagnostics.Add(Error(
                    ManuscriptDiagnosticCodes.InvalidWorkspaceMetadata,
                    $"protocol must be '{ProtocolNames.ProtocolId}'.",
                    path));
                return Result<WorkspaceMetadata>.Fail();
            }

            if (dto.Version != ProtocolNames.SupportedMajorVersion)
            {
                diagnostics.Add(Error(
                    ManuscriptDiagnosticCodes.UnsupportedProtocolVersion,
                    $"Unsupported protocol version {dto.Version}; expected {ProtocolNames.SupportedMajorVersion}.",
                    path));
                return Result<WorkspaceMetadata>.Fail();
            }

            if (dto.Defaults is not null)
                ValidateNestedKeys(path, root, "defaults", ProtocolNames.DefaultsKeys, diagnostics);

            return Result<WorkspaceMetadata>.Ok(new WorkspaceMetadata(
                dto.Protocol,
                dto.Version,
                MapDefaults(dto.Defaults),
                dto.Extensions));
        }
        catch (Exception ex) when (ex is YamlException or InvalidOperationException)
        {
            return FailYaml<WorkspaceMetadata>(ex, diagnostics, path);
        }
    }

    public Result<UniverseMetadata> ReadUniverse(string path, List<ManuscriptDiagnostic> diagnostics) =>
        ReadEntity(path, diagnostics, dto => new UniverseMetadata(dto.Title!, dto.Description, MapDefaults(dto.Defaults), dto.Extensions));

    public Result<SubjectMetadata> ReadSubject(string path, List<ManuscriptDiagnostic> diagnostics) =>
        ReadEntity(path, diagnostics, dto => new SubjectMetadata(dto.Title!, dto.Description, MapDefaults(dto.Defaults), dto.Extensions));

    public Result<SeriesMetadata> ReadSeries(string path, List<ManuscriptDiagnostic> diagnostics) =>
        ReadEntity(path, diagnostics, dto => new SeriesMetadata(dto.Title!, dto.Description, MapDefaults(dto.Defaults), dto.Extensions));

    public Result<BookMetadata> ReadBook(string path, List<ManuscriptDiagnostic> diagnostics)
    {
        if (!TryLoadMapping(path, diagnostics, out var root, out var raw))
            return Result<BookMetadata>.Fail();

        ValidateKeys(path, root, ProtocolNames.BookKeys, diagnostics);
        ValidateNestedKeys(path, root, "targets", ProtocolNames.TargetsKeys, diagnostics);
        ValidateNestedKeys(path, root, "publication", ProtocolNames.PublicationKeys, diagnostics);
        ValidateNestedKeys(path, root, "defaults", ProtocolNames.DefaultsKeys, diagnostics);

        try
        {
            var dto = _deserializer.Deserialize<BookYamlDto>(raw) ?? new BookYamlDto();
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                diagnostics.Add(Error(ManuscriptDiagnosticCodes.MissingBookTitle, "book.yaml requires title.", path));
                return Result<BookMetadata>.Fail();
            }

            return Result<BookMetadata>.Ok(new BookMetadata(
                dto.Title.Trim(),
                NullIfEmpty(dto.Subtitle),
                dto.Order,
                dto.Authors,
                NullIfEmpty(dto.Language),
                NullIfEmpty(dto.Description),
                NullIfEmpty(dto.Rights),
                dto.Targets is null ? null : new TargetsMetadata(dto.Targets.Words),
                dto.Publication is null
                    ? null
                    : new PublicationMetadata(
                        NullIfEmpty(dto.Publication.Version),
                        NullIfEmpty(dto.Publication.Isbn),
                        NullIfEmpty(dto.Publication.Date)),
                dto.Extensions));
        }
        catch (Exception ex) when (ex is YamlException or InvalidOperationException)
        {
            return FailYaml<BookMetadata>(ex, diagnostics, path);
        }
    }

    public Result<ChapterMetadata> ReadChapterFrontMatter(
        string? yamlBlock,
        string path,
        List<ManuscriptDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(yamlBlock))
            return Result<ChapterMetadata>.Ok(new ChapterMetadata());

        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(yamlBlock));
            if (stream.Documents.Count == 0)
                return Result<ChapterMetadata>.Ok(new ChapterMetadata());

            if (stream.Documents[0].RootNode is not YamlMappingNode map)
            {
                diagnostics.Add(Error(ManuscriptDiagnosticCodes.InvalidYaml, "Front matter must be a mapping.", path));
                return Result<ChapterMetadata>.Fail();
            }

            ValidateKeys(path, map, ProtocolNames.ChapterKeys, diagnostics);
            var dto = _deserializer.Deserialize<ChapterYamlDto>(yamlBlock) ?? new ChapterYamlDto();
            var locations = CoerceStringList(dto.Locations) ?? CoerceStringList(dto.Location);
            return Result<ChapterMetadata>.Ok(new ChapterMetadata(
                NullIfEmpty(dto.Status),
                CoerceStringList(dto.Tags),
                CoerceScalar(dto.Date),
                NullIfEmpty(dto.Time),
                CoerceScalar(dto.System),
                locations,
                NullIfEmpty(dto.Pov),
                CoerceStringList(dto.Characters),
                dto.Extensions));
        }
        catch (Exception ex) when (ex is YamlException or InvalidOperationException)
        {
            return FailYaml<ChapterMetadata>(ex, diagnostics, path);
        }
    }

    public Result<ReferenceMetadata> ReadReferenceFrontMatter(
        string? yamlBlock,
        string path,
        List<ManuscriptDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(yamlBlock))
            return Result<ReferenceMetadata>.Ok(new ReferenceMetadata());

        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(yamlBlock));
            if (stream.Documents.Count == 0)
                return Result<ReferenceMetadata>.Ok(new ReferenceMetadata());

            if (stream.Documents[0].RootNode is not YamlMappingNode map)
            {
                diagnostics.Add(Error(ManuscriptDiagnosticCodes.InvalidYaml, "Front matter must be a mapping.", path));
                return Result<ReferenceMetadata>.Fail();
            }

            ValidateKeys(path, map, ProtocolNames.ReferenceKeys, diagnostics);
            var dto = _deserializer.Deserialize<ReferenceYamlDto>(yamlBlock) ?? new ReferenceYamlDto();
            return Result<ReferenceMetadata>.Ok(new ReferenceMetadata(dto.Aliases, dto.Tags, dto.Extensions));
        }
        catch (Exception ex) when (ex is YamlException or InvalidOperationException)
        {
            return FailYaml<ReferenceMetadata>(ex, diagnostics, path);
        }
    }

    Result<T> ReadEntity<T>(string path, List<ManuscriptDiagnostic> diagnostics, Func<EntityYamlDto, T> map)
        where T : class
    {
        if (!TryLoadMapping(path, diagnostics, out var root, out var raw))
            return Result<T>.Fail();

        ValidateKeys(path, root, ProtocolNames.EntityKeys, diagnostics);
        ValidateNestedKeys(path, root, "defaults", ProtocolNames.DefaultsKeys, diagnostics);

        try
        {
            var dto = _deserializer.Deserialize<EntityYamlDto>(raw) ?? new EntityYamlDto();
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                diagnostics.Add(Error(ManuscriptDiagnosticCodes.InvalidYaml, "title is required.", path));
                return Result<T>.Fail();
            }

            return Result<T>.Ok(map(dto));
        }
        catch (Exception ex) when (ex is YamlException or InvalidOperationException)
        {
            return FailYaml<T>(ex, diagnostics, path);
        }
    }

    bool TryLoadMapping(
        string path,
        List<ManuscriptDiagnostic> diagnostics,
        out YamlMappingNode map,
        out string raw)
    {
        map = null!;
        raw = string.Empty;
        if (!File.Exists(path))
            return false;

        try
        {
            raw = File.ReadAllText(path);
            if (raw.StartsWith('\uFEFF'))
                raw = raw[1..];

            var stream = new YamlStream();
            stream.Load(new StringReader(raw));
            if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode mapping)
            {
                diagnostics.Add(Error(ManuscriptDiagnosticCodes.InvalidYaml, "Expected a YAML mapping.", path));
                return false;
            }

            map = mapping;
            return true;
        }
        catch (Exception ex) when (ex is YamlException or IOException)
        {
            diagnostics.Add(Error(ManuscriptDiagnosticCodes.InvalidYaml, ex.Message, path));
            return false;
        }
    }

    [ExcludeFromCodeCoverage(Justification = "YAML exception plumbing shared by readers.")]
    static Result<T> FailYaml<T>(Exception ex, List<ManuscriptDiagnostic> diagnostics, string path)
    {
        diagnostics.Add(Error(ManuscriptDiagnosticCodes.InvalidYaml, ex.Message, path));
        return Result<T>.Fail();
    }

    static void ValidateKeys(
        string path,
        YamlMappingNode map,
        HashSet<string> allowed,
        List<ManuscriptDiagnostic> diagnostics)
    {
        foreach (var key in map.Children.Keys)
        {
            if (key is not YamlScalarNode scalar || scalar.Value is null)
                continue;

            if (!allowed.Contains(scalar.Value))
            {
                diagnostics.Add(Error(
                    ManuscriptDiagnosticCodes.UnknownMetadataField,
                    $"Unknown metadata field '{scalar.Value}'.",
                    path));
            }
        }

        DetectDuplicateKeys(path, map, diagnostics);
    }

    static void ValidateNestedKeys(
        string path,
        YamlMappingNode root,
        string nestedKey,
        HashSet<string> allowed,
        List<ManuscriptDiagnostic> diagnostics)
    {
        foreach (var (keyNode, valueNode) in root.Children)
        {
            if (keyNode is not YamlScalarNode key || key.Value != nestedKey)
                continue;
            if (valueNode is not YamlMappingNode nested)
                return;
            ValidateKeys(path, nested, allowed, diagnostics);
            return;
        }
    }

    static void DetectDuplicateKeys(string path, YamlMappingNode map, List<ManuscriptDiagnostic> diagnostics)
    {
        // YamlDotNet mapping already collapses duplicates; surface explicit duplicate detection via children scan is limited.
        // Keep method for future custom parser if needed.
        _ = path;
        _ = map;
        _ = diagnostics;
    }

    static DefaultsMetadata? MapDefaults(DefaultsYamlDto? dto) =>
        dto is null
            ? null
            : new DefaultsMetadata(dto.Authors, NullIfEmpty(dto.Language), NullIfEmpty(dto.Rights));

    static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    static ManuscriptDiagnostic Error(string code, string message, string path) =>
        new(ManuscriptDiagnosticSeverity.Error, code, message, path);

    public readonly struct Result<T>
    {
        public bool Success { get; }
        public T? Value { get; }

        Result(bool success, T? value)
        {
            Success = success;
            Value = value;
        }

        public static Result<T> Ok(T value) => new(true, value);
        public static Result<T> Fail() => new(false, default);
    }

    sealed class WorkspaceYamlDto
    {
        public string Protocol { get; set; } = "";
        public int Version { get; set; }
        public DefaultsYamlDto? Defaults { get; set; }
        public Dictionary<string, object?>? Extensions { get; set; }
    }

    sealed class EntityYamlDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DefaultsYamlDto? Defaults { get; set; }
        public Dictionary<string, object?>? Extensions { get; set; }
    }

    sealed class BookYamlDto
    {
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public int? Order { get; set; }
        public List<string>? Authors { get; set; }
        public string? Language { get; set; }
        public string? Description { get; set; }
        public string? Rights { get; set; }
        public TargetsYamlDto? Targets { get; set; }
        public PublicationYamlDto? Publication { get; set; }
        public Dictionary<string, object?>? Extensions { get; set; }
    }

    sealed class DefaultsYamlDto
    {
        public List<string>? Authors { get; set; }
        public string? Language { get; set; }
        public string? Rights { get; set; }
    }

    sealed class TargetsYamlDto
    {
        public int? Words { get; set; }
    }

    sealed class PublicationYamlDto
    {
        public string? Version { get; set; }
        public string? Isbn { get; set; }
        public string? Date { get; set; }
    }

    sealed class ChapterYamlDto
    {
        public string? Status { get; set; }
        public object? Tags { get; set; }
        public object? Date { get; set; }
        public string? Time { get; set; }
        public object? System { get; set; }
        public object? Location { get; set; }
        public object? Locations { get; set; }
        public string? Pov { get; set; }
        public object? Characters { get; set; }
        public Dictionary<string, object?>? Extensions { get; set; }
    }

    static string? CoerceScalar(object? value) => value switch
    {
        null => null,
        string s => NullIfEmpty(s),
        _ => NullIfEmpty(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)),
    };

    static List<string>? CoerceStringList(object? value)
    {
        if (value is null)
            return null;
        if (value is string s)
        {
            var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length == 0 ? null : parts.ToList();
        }

        if (value is IEnumerable<object> objs)
        {
            var list = objs.Select(o => Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture)?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToList();
            return list.Count == 0 ? null : list;
        }

        var single = CoerceScalar(value);
        return single is null ? null : [single];
    }

    sealed class ReferenceYamlDto
    {
        public List<string>? Aliases { get; set; }
        public List<string>? Tags { get; set; }
        public Dictionary<string, object?>? Extensions { get; set; }
    }
}
