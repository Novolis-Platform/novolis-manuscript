using Novolis.Markup.Markdown;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Loads reference markdown files as ordered chapter documents, injecting folder titles as H1s.</summary>
static class ReferenceMarkdown
{
    public static IReadOnlyList<ChapterDocument> LoadFiles(IEnumerable<string> files, string contentRoot)
    {
        var chapters = new List<ChapterDocument>();
        var prevRelParts = new List<string>();
        var order = 0;
        foreach (var file in files)
        {
            var relToContent = Path.GetRelativePath(contentRoot, file);
            var dir = Path.GetDirectoryName(relToContent);
            var parts = string.IsNullOrEmpty(dir)
                ? new List<string>()
                : dir.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries).ToList();

            var diverge = 0;
            var minLen = System.Math.Min(parts.Count, prevRelParts.Count);
            while (diverge < minLen && string.Equals(parts[diverge], prevRelParts[diverge], StringComparison.OrdinalIgnoreCase))
                diverge++;

            var body = new MarkdownDocument();
            for (var i = diverge; i < parts.Count; i++)
                body.With(new MarkdownHeader(ToTitleCaseWords(parts[i]), 1));

            prevRelParts = [.. parts];
            if (File.Exists(file))
            {
                var text = File.ReadAllText(file);
                if (text.StartsWith('\uFEFF'))
                    text = text[1..];
                body.With(MarkdownDocument.Parse(text));
            }

            var slug = Path.GetFileNameWithoutExtension(file);
            var title = ToTitleCaseWords(slug);
            chapters.Add(new ChapterDocument(++order, slug, title, ChapterKind.Chapter, Path.GetFullPath(file), body));
        }

        return chapters;
    }

    public static string ToTitleCaseWords(string? folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return "";
        return string.Join(" ", folderName.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Length > 0 ? char.ToUpperInvariant(s[0]) + s[1..] : s));
    }
}
