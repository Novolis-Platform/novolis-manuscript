<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-manuscript/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-manuscript/) · [Source](https://github.com/Novolis-Platform/novolis-manuscript)
<!-- novolis-pkg-brand:end -->

# Novolis.Manuscript.References

Domain-aware **reference library** for manuscript hosts, built on [`Novolis.IO.Indexing`](https://github.com/Novolis-Platform/novolis-io/blob/main/src/Novolis.IO.Indexing/README.md).

- In-memory only
- Open facets / relations (author vocabulary)
- Catalog wiring via `ReferenceSetInfo` / `SeriesInfo` / `BookInfo` **metadata** (ids, titles, paths)
- **Does not** parse Markdown or NMP YAML

## Install

```powershell
dotnet add package Novolis.Manuscript.References
```

## Quick start

```csharp
using Novolis.Manuscript.References;

var library = new ReferenceLibraryBuilder()
    .AddSeries(series) // catalog sets/files only
    .AddCard("calypso", title: "Calypso", aliases: ["the tramp"], facets: new Dictionary<string, IReadOnlyList<string>>
    {
        [ReferenceFacets.Kind] = ["ship"],
    })
    .Mention("chapter:01", "the tramp")
    .Mention("chapter:02", "unknown-slug") // soft / unresolved OK
    .Build();

library.TryResolve("the tramp", out var card);
var debt = library.UnresolvedMentionTargets();
```

## Layering

```text
Novolis.IO.Indexing          format-agnostic graph
Novolis.Manuscript.References   series/book/set cards + vocabulary
Studio / CLI / agents           shallow hosts
```
