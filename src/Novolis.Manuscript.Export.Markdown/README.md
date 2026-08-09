# Novolis.Manuscript.Export.Markdown

Exports manuscript books to reader/author Markdown and HTML using `Novolis.Markup.Markdown` (`Parse` + `MarkdownToHtmlConverter`), no Markdig.

```csharp
var paths = ManuscriptMarkdownExporter.ExportBook(book, outputDir);
// book.reader.md, book.author.md, book.reader.html
```

Reader Markdown strips YAML front matter and private fields (`pov`, `characters`, …). Public keys are emitted as plain `>` value lines (no `[!tag]`). Author Markdown keeps the same public dateline; private fields stay in source YAML only.
