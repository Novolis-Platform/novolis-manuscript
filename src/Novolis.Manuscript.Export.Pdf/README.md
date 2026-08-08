# Novolis.Manuscript.Export.Pdf

QuestPDF book and reference export for manuscripts (cover, H1 page breaks, chapter-metadata filtering), plus Markdown/HTML/TXT companions.

## Install

```powershell
dotnet add package Novolis.Manuscript.Export.Pdf
```

## Studio (single PDF)

```csharp
ManuscriptBookPdfExporter.ExportBook(book, @"D:\out\book.pdf", settings);
ManuscriptBookPdfExporter.ExportReferenceSet(referenceSet, @"D:\out\ref.pdf", settings);
```

## CLI / folder export (md + html + txt + pdf)

```csharp
BookPrintExporter.ExportBookFolder(bookDir, outDir, seriesId: "demo", bookId: "book-one", options);
ReferenceManualExporter.Export(referencesDir, outDir, seriesId: "demo", title: "Reference Manual");
```
