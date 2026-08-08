using System.Text;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Markdown → plain text for TXT companions and QuestPDF inline flattening.</summary>
internal static class PlainTextRenderer
{
    public static void AppendDocument(MarkdownDocument doc, StringBuilder sb, bool showAllTags = false)
    {
        foreach (var block in doc)
            AppendBlock(block, sb, showAllTags);
    }

    public static string InlinesToPlain(ContainerInline? inline, bool preserveLineBreaks = false)
    {
        if (inline == null)
            return "";
        var sb = new StringBuilder();
        AppendInlines(inline, sb, preserveLineBreaks);
        return sb.ToString();
    }

    static void AppendBlock(Block block, StringBuilder sb, bool showAllTags)
    {
        switch (block)
        {
            case HeadingBlock h:
                sb.AppendLine(InlinesToPlain(h.Inline));
                sb.AppendLine();
                break;
            case ParagraphBlock p:
                sb.AppendLine(InlinesToPlain(p.Inline));
                sb.AppendLine();
                break;
            case ThematicBreakBlock:
                sb.AppendLine(new string('-', 20));
                sb.AppendLine();
                break;
            case QuoteBlock q:
                if (ChapterMetadataQuote.TryGetRows(q, out var metadataRows))
                {
                    var visible = ChapterMetadataTagVisibility.FilterForBuild(metadataRows, showAllTags);
                    foreach (var line in ChapterMetadataDisplay.BuildPlainLines(visible, showAllTags))
                        sb.AppendLine(line);

                    if (visible.Count > 0)
                        sb.AppendLine();
                }
                else
                {
                    foreach (var inner in q)
                        AppendBlock(inner, sb, showAllTags);
                }

                break;
            case ListBlock list:
                AppendListPlain(list, sb, showAllTags);
                break;
            case Table table:
                AppendTablePlain(table, sb);
                break;
            case FencedCodeBlock f:
                sb.AppendLine(f.Lines.ToString());
                sb.AppendLine();
                break;
            case CodeBlock c:
                sb.AppendLine(c.Lines.ToString());
                sb.AppendLine();
                break;
            case HtmlBlock:
                break;
            default:
                if (block is ContainerBlock cb)
                {
                    foreach (var inner in cb)
                        AppendBlock(inner, sb, showAllTags);
                }

                break;
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Markdig list marker edge combinations.")]
    static void AppendListPlain(ListBlock list, StringBuilder sb, bool showAllTags)
    {
        var n = 0;
        foreach (var item in list)
        {
            if (item is not ListItemBlock lib)
                continue;
            n++;
            var marker = list.IsOrdered
                ? $"{(lib.Order > 0 ? lib.Order : n)}. "
                : $"{(list.BulletType == '\0' ? '-' : list.BulletType)} ";
            foreach (var inner in lib)
            {
                if (inner is ParagraphBlock pb)
                {
                    sb.Append(marker);
                    marker = new string(' ', marker.Length);
                    AppendInlines(pb.Inline, sb, preserveLineBreaks: false);
                    sb.AppendLine();
                }
                else
                {
                    AppendBlock(inner, sb, showAllTags);
                }
            }

            sb.AppendLine();
        }
    }

    static void AppendTablePlain(Table table, StringBuilder sb)
    {
        foreach (var row in table.OfType<TableRow>())
        {
            var parts = new List<string>();
            foreach (var cell in row.OfType<TableCell>())
                parts.Add(TableCellPlain(cell).ReplaceLineEndings(" ").Trim());
            sb.AppendLine(string.Join(" | ", parts));
        }

        sb.AppendLine();
    }

    internal static string TableCellPlain(TableCell cell)
    {
        var sb = new StringBuilder();
        foreach (var inner in cell)
        {
            if (inner is not ParagraphBlock pb)
                continue;
            if (sb.Length > 0)
                sb.Append(' ');
            AppendInlines(pb.Inline, sb, preserveLineBreaks: false);
        }

        return sb.ToString();
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Markdig inline edge combinations.")]
    static void AppendInlines(ContainerInline? container, StringBuilder sb, bool preserveLineBreaks)
    {
        if (container == null)
            return;
        foreach (var child in container)
        {
            switch (child)
            {
                case LiteralInline lit:
                    sb.Append(lit.Content);
                    break;
                case CodeInline code:
                    sb.Append(code.Content);
                    break;
                case EmphasisInline em:
                    AppendInlines(em, sb, preserveLineBreaks);
                    break;
                case LineBreakInline:
                    sb.Append(preserveLineBreaks ? '\n' : ' ');
                    break;
                case LinkInline link:
                    AppendInlines(link, sb, preserveLineBreaks);
                    break;
                case ContainerInline nested:
                    AppendInlines(nested, sb, preserveLineBreaks);
                    break;
            }
        }
    }
}
