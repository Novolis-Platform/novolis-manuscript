using System.Text.RegularExpressions;

namespace Novolis.Manuscript.Protocol.Internal;

sealed partial class DocumentReader(ProtocolMetadataReader metadataReader)
{
    [GeneratedRegex(@"^(?<order>[1-9][0-9]*)-(?<slug>[a-z0-9]+(?:-[a-z0-9]+)*)\.md$", RegexOptions.CultureInvariant)]
    private static partial Regex DocumentFileName();

    public IReadOnlyList<ManuscriptDocument> ReadDocuments(
        string directory,
        ManuscriptDocumentKind kind,
        List<ManuscriptDiagnostic> diagnostics)
    {
        if (!Directory.Exists(directory))
            return [];

        var results = new List<ManuscriptDocument>();
        var seenOrders = new Dictionary<int, string>();

        foreach (var file in Directory.GetFiles(directory, "*.md").OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(file);
            var match = DocumentFileName().Match(name);
            if (!match.Success)
            {
                diagnostics.Add(new ManuscriptDiagnostic(
                    ManuscriptDiagnosticSeverity.Error,
                    ManuscriptDiagnosticCodes.InvalidDocumentFilename,
                    $"Invalid document filename '{name}'. Expected <number>-<slug>.md.",
                    file));
                continue;
            }

            var order = int.Parse(match.Groups["order"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var slug = match.Groups["slug"].Value;

            if (seenOrders.TryGetValue(order, out var prior))
            {
                diagnostics.Add(new ManuscriptDiagnostic(
                    ManuscriptDiagnosticSeverity.Error,
                    ManuscriptDiagnosticCodes.DuplicateDocumentOrder,
                    $"Duplicate document order {order} (also '{prior}').",
                    file));
            }
            else
            {
                seenOrders[order] = name;
            }

            var text = File.ReadAllText(file);
            if (text.StartsWith('\uFEFF'))
                text = text[1..];

            var (frontMatter, body) = SplitFrontMatter(text);
            var metaResult = metadataReader.ReadChapterFrontMatter(frontMatter, file, diagnostics);
            var metadata = metaResult.Success ? metaResult.Value! : new ChapterMetadata();

            var title = ReadFirstH1(body);
            if (string.IsNullOrWhiteSpace(title))
            {
                diagnostics.Add(new ManuscriptDiagnostic(
                    ManuscriptDiagnosticSeverity.Error,
                    ManuscriptDiagnosticCodes.MissingDocumentTitle,
                    "Document requires a non-empty level-one heading.",
                    file));
                title = slug;
            }

            results.Add(new ManuscriptDocument(slug, order, title.Trim(), kind, file, metadata));
        }

        return results.OrderBy(d => d.Order).ThenBy(d => d.Slug, StringComparer.Ordinal).ToList();
    }

    internal static (string? FrontMatter, string Body) SplitFrontMatter(string text)
    {
        var normalized = text.Replace("\r\n", "\n");
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
            return (null, text);

        var end = normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (end < 0)
        {
            // Allow trailing --- at EOF
            end = normalized.IndexOf("\n---", 4, StringComparison.Ordinal);
            if (end < 0 || end + 4 != normalized.Length)
                return (null, text);
            return (normalized[4..end], string.Empty);
        }

        var yaml = normalized[4..end];
        var body = normalized[(end + 5)..];
        return (yaml, body);
    }

    internal static string? ReadFirstH1(string body)
    {
        foreach (var line in body.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            if (line.StartsWith("# ", StringComparison.Ordinal))
                return line[2..].Trim();
            // First non-empty non-H1 line means no title
            return null;
        }

        return null;
    }
}
