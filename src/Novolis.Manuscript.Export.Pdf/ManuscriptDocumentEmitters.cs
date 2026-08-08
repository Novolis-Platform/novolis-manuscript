using System.Net;
using System.Text;
using Markdig;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Companion Markdown / HTML / TXT emitters for print builds.</summary>
internal static class ManuscriptDocumentEmitters
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "BOM strip on legacy concat path.")]
    public static string ConcatenateChapterMarkdown(IEnumerable<string> paths)
    {
        var sb = new StringBuilder();
        foreach (var input in paths)
        {
            if (!File.Exists(input))
                continue;
            var body = File.ReadAllText(input);
            if (body.StartsWith('\uFEFF'))
                body = body[1..];
            sb.Append(body);
            if (!body.EndsWith('\n'))
                sb.AppendLine();
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static void WriteMarkdown(string markdown, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, markdown, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public static void WriteHtml(
        string title,
        string markdown,
        string htmlPath,
        string? stylesheetPath,
        bool showAllTags,
        IReadOnlyDictionary<string, string>? meta = null)
    {
        var bodyHtml = Markdown.ToHtml(markdown, MarkdownRenderPipeline.Instance);
        bodyHtml = ChapterMetadataHtml.TransformBlockquotes(bodyHtml, showAllTags);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine($"  <title>{WebUtility.HtmlEncode(title)}</title>");
        if (!string.IsNullOrWhiteSpace(stylesheetPath) && File.Exists(stylesheetPath))
        {
            var cssUri = new Uri(Path.GetFullPath(stylesheetPath)).AbsoluteUri;
            sb.AppendLine($"  <link rel=\"stylesheet\" href=\"{WebUtility.HtmlEncode(cssUri)}\" />");
        }
        else
        {
            sb.AppendLine("  <style>body{font-family:Georgia,serif;max-width:40rem;margin:2rem auto;line-height:1.45;padding:0 1rem}</style>");
        }

        if (meta != null)
        {
            foreach (var k in new[] { "author", "series" })
            {
                if (meta.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    sb.AppendLine($"  <meta name=\"{k}\" content=\"{WebUtility.HtmlEncode(v)}\" />");
            }
        }

        sb.AppendLine("</head>");
        sb.AppendLine(showAllTags ? "<body class=\"debug-mode\">" : "<body>");
        sb.AppendLine(bodyHtml);
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(htmlPath))!);
        File.WriteAllText(htmlPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public static void WritePlainText(string markdown, string txtPath, bool showAllTags)
    {
        var doc = Markdown.Parse(markdown, MarkdownRenderPipeline.Instance);
        var sb = new StringBuilder();
        PlainTextRenderer.AppendDocument(doc, sb, showAllTags);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(txtPath))!);
        File.WriteAllText(txtPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
