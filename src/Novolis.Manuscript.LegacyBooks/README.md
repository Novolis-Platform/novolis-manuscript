<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-manuscript/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-manuscript/) · [Source](https://github.com/Novolis-Platform/novolis-manuscript)
<!-- novolis-pkg-brand:end -->

# Novolis.Manuscript.LegacyBooks

Isolated adapter for the pre-NMP layout (`content/series`, `content/books`, lowercase `chapters`, `references`/`reference`, callouts, `chapter_order_from_heading`). Returns the same `ManuscriptSnapshot` as NMP/1 without changing Protocol rules.

## Install

```bash
dotnet add package Novolis.Manuscript.LegacyBooks
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`). Depends on `Novolis.Manuscript.Protocol`.

## Mapping notes

- Fiction series under `content/series/<id>` are placed under a synthetic universe id `legacy` (title `Legacy`). Protocol itself still requires real `universe.yaml` for NMP/1 trees.
- Standalone books under `content/books/<id>` become NonFiction under synthetic subject id `legacy` (title `Legacy`).
- Renderer flags such as `debug_mode` are ignored for the protocol catalog.

## Quick start

```csharp
using Novolis.Manuscript.LegacyBooks;

var snapshot = new LegacyBooksCatalogReader().Read(root);
```
