using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Metrics;

/// <summary>Chapter row used for character slice reports.</summary>
public sealed record CharacterSliceChapter(
    string FileName,
    int? Number,
    string Title,
    string? PovRaw,
    string? CharactersRaw,
    IReadOnlyList<string> PovNames,
    IReadOnlyList<string> CharacterNames);

/// <summary>One character's POV and cast chapter lists.</summary>
public sealed class CharacterSlice
{
    /// <summary>Chapters where this name appears in POV.</summary>
    public List<CharacterSliceChapter> Pov { get; } = [];

    /// <summary>Chapters where this name appears in cast/characters.</summary>
    public List<CharacterSliceChapter> Characters { get; } = [];
}

/// <summary>Aggregated character-slice report for a book.</summary>
public sealed class CharacterSliceReport
{
    /// <summary>Display label (usually book id).</summary>
    public required string Label { get; init; }

    /// <summary>Chapters directory scanned.</summary>
    public required string ChaptersDir { get; init; }

    /// <summary>All scanned chapters.</summary>
    public required IReadOnlyList<CharacterSliceChapter> Chapters { get; init; }

    /// <summary>Chapters missing POV metadata.</summary>
    public required IReadOnlyList<CharacterSliceChapter> MissingPov { get; init; }

    /// <summary>Chapters missing characters metadata.</summary>
    public required IReadOnlyList<CharacterSliceChapter> MissingCharacters { get; init; }

    /// <summary>Per-character slices (case-insensitive keys).</summary>
    public required IReadOnlyDictionary<string, CharacterSlice> Characters { get; init; }

    /// <summary>Renders the Markdown report (optionally filtered to one character).</summary>
    public string ToMarkdown(string? characterFilter = null)
    {
        IEnumerable<KeyValuePair<string, CharacterSlice>> slices = Characters
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(characterFilter))
        {
            if (!Characters.TryGetValue(characterFilter.Trim(), out var match))
                throw new InvalidOperationException($"Character not found in metadata: {characterFilter}");
            slices = [new KeyValuePair<string, CharacterSlice>(characterFilter.Trim(), match)];
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# {Label} character slices");
        sb.AppendLine();
        sb.AppendLine($"Scanned `{Chapters.Count}` chapters from `{ChaptersDir}`.");
        sb.AppendLine();
        sb.AppendLine("## Coverage");
        sb.AppendLine();
        sb.AppendLine($"- Missing `pov`: {MissingPov.Count}");
        sb.AppendLine($"- Missing `characters`: {MissingCharacters.Count}");
        sb.AppendLine($"- Distinct named characters in metadata: {Characters.Count}");
        sb.AppendLine();

        if (MissingPov.Count > 0)
        {
            sb.AppendLine("### Missing POV");
            sb.AppendLine();
            foreach (var ch in MissingPov)
                sb.AppendLine($"- Ch. {FmtNum(ch)} - {ch.Title} (`{ch.FileName}`)");
            sb.AppendLine();
        }

        if (MissingCharacters.Count > 0)
        {
            sb.AppendLine("### Missing Characters");
            sb.AppendLine();
            foreach (var ch in MissingCharacters)
                sb.AppendLine($"- Ch. {FmtNum(ch)} - {ch.Title} (`{ch.FileName}`)");
            sb.AppendLine();
        }

        foreach (var (name, slice) in slices)
        {
            sb.AppendLine($"## {name}");
            sb.AppendLine();
            sb.AppendLine($"- POV chapters: {slice.Pov.Count}");
            sb.AppendLine($"- On-stage / cast chapters: {slice.Characters.Count}");
            sb.AppendLine();

            if (slice.Pov.Count > 0)
            {
                sb.AppendLine("### POV");
                sb.AppendLine();
                foreach (var ch in slice.Pov.OrderBy(c => c.Number ?? int.MaxValue).ThenBy(c => c.FileName, StringComparer.Ordinal))
                    sb.AppendLine($"- Ch. {FmtNum(ch)} - {ch.Title} (`{ch.FileName}`)");
                sb.AppendLine();
            }

            if (slice.Characters.Count > 0)
            {
                sb.AppendLine("### Cast");
                sb.AppendLine();
                foreach (var ch in slice.Characters.OrderBy(c => c.Number ?? int.MaxValue).ThenBy(c => c.FileName, StringComparer.Ordinal))
                    sb.AppendLine($"- Ch. {FmtNum(ch)} - {ch.Title} (`{ch.FileName}`)");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>Serializes a compact JSON summary of the report.</summary>
    public string ToJson(string? characterFilter = null)
    {
        IEnumerable<KeyValuePair<string, CharacterSlice>> slices = Characters
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(characterFilter))
        {
            if (!Characters.TryGetValue(characterFilter.Trim(), out var match))
                throw new InvalidOperationException($"Character not found in metadata: {characterFilter}");
            slices = [new KeyValuePair<string, CharacterSlice>(characterFilter.Trim(), match)];
        }

        var payload = new
        {
            label = Label,
            chaptersDir = ChaptersDir,
            chapterCount = Chapters.Count,
            missingPov = MissingPov.Count,
            missingCharacters = MissingCharacters.Count,
            characters = slices.Select(kv => new
            {
                name = kv.Key,
                pov = kv.Value.Pov.Count,
                cast = kv.Value.Characters.Count,
            }),
        };
        return System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    static string FmtNum(CharacterSliceChapter ch) =>
        ch.Number?.ToString(CultureInfo.InvariantCulture) ?? "?";
}

/// <summary>Builds character slice reports from chapter opening metadata (<c>[!pov]</c>, <c>[!characters]</c>).</summary>
public static class ManuscriptCharacterSlices
{
    static readonly Regex DividerRx = new(@"\s*(?:/|;|,)\s*", RegexOptions.Compiled);

    /// <summary>Scans a chapters directory and builds a report.</summary>
    public static CharacterSliceReport Build(string label, string chaptersDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chaptersDir);
        if (!Directory.Exists(chaptersDir))
            throw new DirectoryNotFoundException(chaptersDir);

        var chapters = Scan(chaptersDir);
        if (chapters.Count == 0)
            throw new InvalidOperationException($"No chapter markdown files found in {chaptersDir}");

        var allCharacters = new Dictionary<string, CharacterSlice>(StringComparer.OrdinalIgnoreCase);
        foreach (var ch in chapters)
        {
            foreach (var name in ch.PovNames)
                Get(allCharacters, name).Pov.Add(ch);
            foreach (var name in ch.CharacterNames)
                Get(allCharacters, name).Characters.Add(ch);
        }

        return new CharacterSliceReport
        {
            Label = string.IsNullOrWhiteSpace(label) ? Path.GetFileName(Directory.GetParent(chaptersDir)?.FullName ?? chaptersDir)! : label,
            ChaptersDir = chaptersDir,
            Chapters = chapters,
            MissingPov = chapters.Where(c => string.IsNullOrWhiteSpace(c.PovRaw)).ToList(),
            MissingCharacters = chapters.Where(c => string.IsNullOrWhiteSpace(c.CharactersRaw)).ToList(),
            Characters = allCharacters,
        };
    }

    /// <summary>Builds a report for a loaded book.</summary>
    public static CharacterSliceReport Build(BookInfo book)
    {
        ArgumentNullException.ThrowIfNull(book);
        return Build(book.Id, ManuscriptPaths.ResolveChaptersDirectory(book));
    }

    /// <summary>Builds a report from workspace + series/book ids.</summary>
    public static CharacterSliceReport BuildFromWorkspace(string workspaceRoot, string? seriesId, string bookId)
    {
        var (book, _) = ManuscriptPaths.ResolveBookChapters(workspaceRoot, seriesId, bookId);
        return Build(book);
    }

    /// <summary>Scans chapters and returns chapter rows (no aggregation).</summary>
    public static IReadOnlyList<CharacterSliceChapter> Scan(string chaptersDir)
    {
        var files = Directory.GetFiles(chaptersDir, "*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToList();
        var list = new List<CharacterSliceChapter>();
        foreach (var path in files)
        {
            var raw = File.ReadAllText(path);
            var (meta, _, format) = ManuscriptMetadata.Parse(raw);
            if (format == ManuscriptMetadataFormat.None && string.IsNullOrWhiteSpace(meta.Number))
                continue;

            int? number = null;
            if (!string.IsNullOrWhiteSpace(meta.Number)
                && int.TryParse(meta.Number.Split('.')[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                number = n;

            list.Add(new CharacterSliceChapter(
                Path.GetFileName(path),
                number,
                meta.Title?.Trim() ?? "",
                meta.Pov,
                meta.Characters,
                SplitNames(meta.Pov),
                SplitNames(meta.Characters)));
        }

        return list;
    }

    static IReadOnlyList<string> SplitNames(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];
        return DividerRx.Split(raw.Trim())
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    static CharacterSlice Get(Dictionary<string, CharacterSlice> map, string name)
    {
        if (!map.TryGetValue(name, out var slice))
        {
            slice = new CharacterSlice();
            map[name] = slice;
        }

        return slice;
    }
}
