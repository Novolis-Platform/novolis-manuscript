using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Renders Markdig blocks into a QuestPDF column (books + reference fidelity).</summary>
internal static class QuestPdfBlockRenderer
{
    /// <summary>Appends a block. Returns true when material was written to the column.</summary>
    public static bool AppendBlock(
        ColumnDescriptor col,
        Block block,
        bool showAllTags,
        ManuscriptPrintSettings style)
    {
        switch (block)
        {
            case HeadingBlock h:
                AppendHeading(col, h, style);
                return true;
            case ParagraphBlock p:
                col.Item().AlignLeft().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(style.BodyFontSize).FontFamily(style.BodyFontFamily)
                        .LineHeight(style.LineHeight));
                    t.Span(PlainTextRenderer.InlinesToPlain(p.Inline));
                });
                return true;
            case ThematicBreakBlock:
                col.Item().PaddingTop(18).PaddingBottom(18).AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(style.SceneBreakSizePt).FontFamily(style.BodyFontFamily)
                        .FontColor(Colors.Grey.Darken1));
                    t.Span("***");
                });
                return true;
            case QuoteBlock q:
                if (ChapterMetadataQuote.TryGetRows(q, out var rows))
                {
                    var visible = ChapterMetadataTagVisibility.FilterForBuild(rows, showAllTags);
                    if (visible.Count > 0)
                    {
                        AppendChapterMetadataQuote(col, visible, showAllTags);
                        return true;
                    }

                    return false;
                }

                if (IsDatelineStyleQuote(q))
                {
                    AppendDatelineQuote(col, q);
                    return true;
                }

                AppendCalloutQuote(col, q, showAllTags, style);
                return true;
            case ListBlock list:
                AppendList(col, list, showAllTags, style);
                return true;
            case Table table:
                AppendTable(col, table, style);
                return true;
            case HtmlBlock:
            case BlankLineBlock:
                return false;
            case FencedCodeBlock f:
                col.Item().Background(Colors.Grey.Lighten4).Padding(6)
                    .Text(f.Lines.ToString()).FontSize(9).FontFamily(style.CodeFontFamily);
                return true;
            case CodeBlock c:
                col.Item().Background(Colors.Grey.Lighten4).Padding(6)
                    .Text(c.Lines.ToString()).FontSize(9).FontFamily(style.CodeFontFamily);
                return true;
            default:
                if (block is ContainerBlock cb)
                {
                    var any = false;
                    foreach (var inner in cb)
                    {
                        if (AppendBlock(col, inner, showAllTags, style))
                            any = true;
                    }

                    return any;
                }

                return false;
        }
    }

    static void AppendHeading(ColumnDescriptor col, HeadingBlock h, ManuscriptPrintSettings style)
    {
        var plain = PlainTextRenderer.InlinesToPlain(h.Inline);
        switch (h.Level)
        {
            case 1:
                col.Item().PaddingTop(32).PaddingBottom(26).AlignCenter()
                    .Text(plain).FontSize(style.ChapterTitleSizePt).FontFamily(style.BodyFontFamily);
                break;
            case 2:
                col.Item().PaddingTop(12).PaddingBottom(8).AlignLeft()
                    .Text(plain).FontSize(style.H2SizePt).FontFamily(style.BodyFontFamily).Bold();
                break;
            default:
                col.Item().PaddingTop(10).PaddingBottom(6).AlignLeft()
                    .Text(plain).FontSize(style.H3SizePt).FontFamily(style.BodyFontFamily).SemiBold();
                break;
        }
    }

    static void AppendChapterMetadataQuote(
        ColumnDescriptor col,
        List<(string Tag, string Value)> rows,
        bool debugMode)
    {
        var lines = ChapterMetadataDisplay.BuildPlainLines(rows, debugMode);
        if (lines.Count == 0)
            return;

        col.Item().PaddingTop(6).PaddingBottom(12)
            .Background(Colors.Grey.Lighten4)
            .Border(0.8f).BorderColor(Colors.Grey.Darken1)
            .Padding(6)
            .Column(panel =>
            {
                panel.Spacing(1.5f);
                foreach (var line in lines)
                {
                    panel.Item().AlignLeft()
                        .Text(line).FontSize(8.5f).FontFamily("Consolas").LineHeight(1.22f)
                        .FontColor(Colors.Grey.Darken4);
                }
            });
    }

    static void AppendDatelineQuote(ColumnDescriptor col, QuoteBlock q)
    {
        col.Item().PaddingTop(8).PaddingBottom(14).PaddingLeft(8).BorderLeft(1).BorderColor(Colors.Grey.Lighten2)
            .Column(qc =>
            {
                qc.Spacing(6);
                foreach (var inner in q)
                {
                    if (inner is not ParagraphBlock pb)
                        continue;
                    qc.Item().AlignLeft().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(9.5f).FontFamily(Fonts.CourierNew).LineHeight(1.35f)
                            .FontColor(Colors.Grey.Darken3));
                        t.Span(PlainTextRenderer.InlinesToPlain(pb.Inline, preserveLineBreaks: true).TrimEnd());
                    });
                }
            });
    }

    static void AppendCalloutQuote(
        ColumnDescriptor col,
        QuoteBlock q,
        bool showAllTags,
        ManuscriptPrintSettings style)
    {
        col.Item().PaddingLeft(14).PaddingTop(4).PaddingBottom(8).BorderLeft(2).BorderColor(Colors.Grey.Lighten1)
            .Column(qc =>
            {
                qc.Spacing(6);
                foreach (var inner in q)
                {
                    switch (inner)
                    {
                        case ParagraphBlock pb:
                            qc.Item().AlignLeft().Text(t =>
                            {
                                t.DefaultTextStyle(s => s.FontSize(style.BodyFontSize).FontFamily(style.BodyFontFamily)
                                    .LineHeight(style.LineHeight));
                                t.Span(PlainTextRenderer.InlinesToPlain(pb.Inline));
                            });
                            break;
                        default:
                            AppendBlock(qc, inner, showAllTags, style);
                            break;
                    }
                }
            });
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "QuestPDF list marker edge combinations.")]
    static void AppendList(
        ColumnDescriptor col,
        ListBlock list,
        bool showAllTags,
        ManuscriptPrintSettings style)
    {
        var n = 0;
        foreach (var item in list)
        {
            if (item is not ListItemBlock lib)
                continue;
            n++;
            var marker = list.IsOrdered
                ? $"{(lib.Order > 0 ? lib.Order : n)}."
                : (list.BulletType == '\0' ? "-" : list.BulletType.ToString());
            var markerWidth = list.IsOrdered ? 34f : 18f;

            col.Item().Row(row =>
            {
                row.ConstantItem(markerWidth).AlignTop().PaddingRight(4).Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(style.BodyFontSize).FontFamily(style.BodyFontFamily)
                        .LineHeight(style.LineHeight));
                    t.Span(marker);
                });
                row.RelativeItem().AlignTop().Column(itemCol =>
                {
                    itemCol.Spacing(4);
                    foreach (var inner in lib)
                        AppendBlock(itemCol, inner, showAllTags, style);
                });
            });
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "QuestPDF table layout; hard to hit empty-row AST edges.")]
    static void AppendTable(ColumnDescriptor col, Table table, ManuscriptPrintSettings style)
    {
        var rows = table.OfType<TableRow>().ToList();
        if (rows.Count == 0)
            return;

        var colCount = 0;
        foreach (var row in rows)
            colCount = Math.Max(colCount, row.OfType<TableCell>().Count());
        if (colCount == 0)
            return;

        var headerRows = new List<TableRow>();
        var bodyRows = new List<TableRow>();
        var stillHeader = true;
        foreach (var row in rows)
        {
            if (row.IsHeader && stillHeader)
                headerRows.Add(row);
            else
            {
                stillHeader = false;
                bodyRows.Add(row);
            }
        }

        if (bodyRows.Count == 0 && headerRows.Count > 0)
        {
            bodyRows.AddRange(headerRows);
            headerRows.Clear();
        }

        col.Item().PaddingTop(6).PaddingBottom(10).Table(t =>
        {
            t.ColumnsDefinition(columns =>
            {
                for (var i = 0; i < colCount; i++)
                    columns.RelativeColumn();
            });

            static IContainer TableCellBox(IContainer c) =>
                c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4);

            if (headerRows.Count > 0)
            {
                t.Header(header =>
                {
                    foreach (var hRow in headerRows)
                    {
                        var cells = hRow.OfType<TableCell>().ToList();
                        for (var i = 0; i < colCount; i++)
                        {
                            var text = i < cells.Count ? PlainTextRenderer.TableCellPlain(cells[i]) : "";
                            header.Cell().Element(TableCellBox).Background(Colors.Grey.Lighten4).AlignLeft().Text(tx =>
                            {
                                tx.DefaultTextStyle(s => s.FontSize(10).FontFamily(style.BodyFontFamily).SemiBold()
                                    .LineHeight(style.LineHeight));
                                tx.Span(text);
                            });
                        }
                    }
                });
            }

            foreach (var row in bodyRows)
            {
                var cells = row.OfType<TableCell>().ToList();
                for (var i = 0; i < colCount; i++)
                {
                    var text = i < cells.Count ? PlainTextRenderer.TableCellPlain(cells[i]) : "";
                    t.Cell().Element(TableCellBox).AlignLeft().Text(tx =>
                    {
                        tx.DefaultTextStyle(s => s.FontSize(style.BodyFontSize).FontFamily(style.BodyFontFamily)
                            .LineHeight(style.LineHeight));
                        tx.Span(text);
                    });
                }
            }
        });
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Dateline vs dialogue quote heuristic.")]
    static bool IsDatelineStyleQuote(QuoteBlock q)
    {
        if (QuoteContainsStrongEmphasis(q))
            return false;
        if (QuoteContainsDialogueLead(q))
            return false;
        var full = QuotePlainCombined(q);
        return full.Length > 0 && full.Length <= 380;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Quote heuristic helpers.")]
    static string QuotePlainCombined(QuoteBlock q)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var inner in q)
        {
            if (inner is not ParagraphBlock pb)
                continue;
            var line = PlainTextRenderer.InlinesToPlain(pb.Inline).Trim();
            if (line.Length == 0)
                continue;
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append(line);
        }

        return sb.ToString();
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Quote heuristic helpers.")]
    static bool QuoteContainsDialogueLead(QuoteBlock q)
    {
        foreach (var inner in q)
        {
            if (inner is not ParagraphBlock pb)
                continue;
            var t = PlainTextRenderer.InlinesToPlain(pb.Inline).TrimStart();
            if (t.Length > 0 && t[0] == '"')
                return true;
        }

        return false;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Quote heuristic helpers.")]
    static bool QuoteContainsStrongEmphasis(QuoteBlock q)
    {
        foreach (var inner in q)
        {
            if (inner is ParagraphBlock pb && ContainerHasStrongEmphasis(pb.Inline))
                return true;
        }

        return false;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Quote heuristic helpers.")]
    static bool ContainerHasStrongEmphasis(ContainerInline? c)
    {
        if (c == null)
            return false;
        foreach (var child in c)
        {
            if (child is EmphasisInline em && em.DelimiterCount >= 2)
                return true;
            if (child is ContainerInline nested && ContainerHasStrongEmphasis(nested))
                return true;
        }

        return false;
    }
}
