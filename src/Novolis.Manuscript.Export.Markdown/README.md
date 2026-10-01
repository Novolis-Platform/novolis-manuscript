<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-manuscript/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-manuscript/) · [Source](https://github.com/Novolis-Platform/novolis-manuscript)
<!-- novolis-pkg-brand:end -->

# Novolis.Manuscript.Export.Markdown

Exports manuscript books to reader/author Markdown and HTML using `Novolis.Markup.Markdown` (`Parse` + `MarkdownToHtmlConverter`), no Markdig.

## Install

```powershell
dotnet add package Novolis.Manuscript.Export.Markdown
```

## Usage

```csharp
var paths = ManuscriptMarkdownExporter.ExportBook(book, outputDir);
// book.reader.md, book.author.md, book.reader.html
```

Reader Markdown strips YAML front matter and private fields (`pov`, `characters`, …). Public keys are emitted as plain `>` value lines (no `[!tag]`). Author Markdown keeps the same public dateline; private fields stay in source YAML only.

