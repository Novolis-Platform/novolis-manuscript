<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-manuscript">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Manuscript.Export.Pdf

Book and reference PDF export for manuscripts via `Novolis.Documents` + `Novolis.Documents.Skia` (cover, chapter headers, chapter-metadata filtering), plus Markdown/HTML/TXT companions via `Novolis.Markup.Markdown` (no Markdig, no QuestPDF).

## Install

```powershell
dotnet add package Novolis.Manuscript.Export.Pdf
```

## Usage

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

