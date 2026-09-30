using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Metrics;

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
