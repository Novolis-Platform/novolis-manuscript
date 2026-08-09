using System.Net;
using System.Text;
using Novolis.Markup.Markdown;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Companion Markdown / HTML / TXT emitters for print builds (Novolis Markdown, not Markdig).</summary>
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
        var document = MarkdownDocument.Parse(markdown);
        var bodyHtml = RenderHtmlBody(document, showAllTags);

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
            sb.AppendLine("  <style>body{font-family:Georgia,serif;max-width:40rem;margin:2rem auto;line-height:1.45;padding:0 1rem}"
                + "blockquote.chapter-metadata{background:#f4f4f4;border:1px solid #999;padding:6px 10px;font-family:Consolas,monospace;font-size:0.85rem}</style>");
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
        var document = MarkdownDocument.Parse(markdown);
        var sb = new StringBuilder();
        var pending = new List<(string Tag, string Value)>();
        var allowDateline = true;

        void Flush()
        {
            if (pending.Count == 0)
                return;
            var visible = ChapterMetadataTagVisibility.FilterForBuild(pending, showAllTags);
            pending.Clear();
            if (visible.Count == 0)
                return;
            foreach (var line in ChapterMetadataDisplay.BuildPlainLines(visible, showAllTags))
                sb.AppendLine(line);
            sb.AppendLine();
        }

        foreach (var section in document)
        {
            if (section is IMarkdownHeader { Level: 1 })
            {
                Flush();
                ManuscriptPlainTextRenderer.AppendSection(section, sb);
                allowDateline = true;
                continue;
            }

            if (allowDateline && ChapterMetadataQuote.TryGetRows(section, pending.Count > 0, out var rows))
            {
                pending.AddRange(rows);
                continue;
            }

            allowDateline = false;
            Flush();
            ManuscriptPlainTextRenderer.AppendSection(section, sb);
        }

        Flush();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(txtPath))!);
        File.WriteAllText(txtPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    static string RenderHtmlBody(IMarkdownDocument document, bool showAllTags)
    {
        var sb = new StringBuilder();
        var pending = new List<(string Tag, string Value)>();
        var allowDateline = true;

        void Flush()
        {
            if (pending.Count == 0)
                return;
            var visible = ChapterMetadataTagVisibility.FilterForBuild(pending, showAllTags);
            pending.Clear();
            if (visible.Count == 0)
                return;
            var lines = ChapterMetadataDisplay.BuildPlainLines(visible, showAllTags);
            if (lines.Count == 0)
                return;
            sb.Append("<blockquote class=\"chapter-metadata\">");
            foreach (var line in lines)
                sb.Append("<p>").Append(WebUtility.HtmlEncode(line)).Append("</p>");
            sb.Append("</blockquote>\n");
        }

        foreach (var section in document)
        {
            if (section is IMarkdownHeader { Level: 1 })
            {
                Flush();
                sb.Append(MarkdownToHtmlConverter.Convert(MarkdownDocument.Create(section))).Append('\n');
                allowDateline = true;
                continue;
            }

            if (allowDateline && ChapterMetadataQuote.TryGetRows(section, pending.Count > 0, out var rows))
            {
                pending.AddRange(rows);
                continue;
            }

            allowDateline = false;
            Flush();
            sb.Append(MarkdownToHtmlConverter.Convert(MarkdownDocument.Create(section))).Append('\n');
        }

        Flush();
        return sb.ToString();
    }
}
