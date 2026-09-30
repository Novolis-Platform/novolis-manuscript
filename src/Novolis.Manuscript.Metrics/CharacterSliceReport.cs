using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Metrics;

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
