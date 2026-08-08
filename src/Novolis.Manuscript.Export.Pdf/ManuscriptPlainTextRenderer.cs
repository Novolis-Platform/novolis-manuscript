using System.Text;
using Novolis.Markup.Markdown;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Renders a Novolis <see cref="IMarkdownDocument"/> to plain text for TXT companions.</summary>
internal static class ManuscriptPlainTextRenderer
{
    public static void AppendDocument(IMarkdownDocument document, StringBuilder sb)
    {
        foreach (var section in document)
            AppendSection(section, sb);
    }

    public static void AppendSection(IMarkdownSection section, StringBuilder sb)
    {
        switch (section)
        {
            case IMarkdownHeader header:
                sb.AppendLine(header.Text);
                sb.AppendLine();
                break;
            case IMarkdownParagraph paragraph:
                sb.AppendLine(FlattenParagraph(paragraph));
                sb.AppendLine();
                break;
            case IMarkdownHorizontalRule:
                sb.AppendLine(new string('-', 20));
                sb.AppendLine();
                break;
            case IMarkdownCodeBlock code:
                sb.AppendLine(code.Code);
                sb.AppendLine();
                break;
            case IMarkdownAlert alert:
                foreach (var line in alert.Text)
                    sb.AppendLine(line);
                sb.AppendLine();
                break;
            case IMarkdownQuote quote:
                foreach (var line in quote.Text)
                    sb.AppendLine("> " + line);
                sb.AppendLine();
                break;
            case IMarkdownUnorderedList list:
                foreach (var item in list.Items)
                    sb.AppendLine("- " + item);
                sb.AppendLine();
                break;
            case IMarkdownOrderedList list:
                var n = 1;
                foreach (var item in list.Items)
                    sb.AppendLine($"{n++}. {item}");
                sb.AppendLine();
                break;
            case IMarkdownTable table:
                AppendTable(table, sb);
                break;
        }
    }

    internal static string FlattenParagraph(IMarkdownParagraph paragraph)
    {
        var sb = new StringBuilder();
        string? pendingLinkText = null;
        foreach (var item in paragraph.Items)
        {
            switch (item.Type)
            {
                case MarkdownParagraphItemType.LinkText:
                    pendingLinkText = item.Text;
                    break;
                case MarkdownParagraphItemType.Link:
                    sb.Append(pendingLinkText ?? string.Empty);
                    pendingLinkText = null;
                    break;
                case MarkdownParagraphItemType.NewLine:
                    sb.Append(' ');
                    break;
                default:
                    if (pendingLinkText is not null)
                    {
                        sb.Append(pendingLinkText);
                        pendingLinkText = null;
                    }

                    sb.Append(item.Text);
                    break;
            }
        }

        if (pendingLinkText is not null)
            sb.Append(pendingLinkText);
        return sb.ToString();
    }

    static void AppendTable(IMarkdownTable table, StringBuilder sb)
    {
        var headers = table.Headers.ToArray();
        if (headers.Length > 0)
            sb.AppendLine(string.Join(" | ", headers));
        foreach (var row in table.Rows)
            sb.AppendLine(string.Join(" | ", row));
        sb.AppendLine();
    }
}
