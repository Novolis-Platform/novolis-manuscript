# Novolis.Manuscript.Export.Markdown

Exports manuscript books to reader/author Markdown and HTML using `Novolis.Markup.Markdown` (`Parse` + `MarkdownToHtmlConverter`), no Markdig.

```csharp
var paths = ManuscriptMarkdownExporter.ExportBook(book, outputDir);
// book.reader.md, book.author.md, book.reader.html
```

Reader Markdown strips YAML front matter and hidden fields (`pov`, `characters`, …). Author Markdown includes hidden metadata as callouts.
