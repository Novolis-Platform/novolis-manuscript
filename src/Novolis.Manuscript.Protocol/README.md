<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-markup">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Manuscript.Protocol

Typed reader for **Novolis Manuscript Protocol 1 (NMP/1)**: workspace discovery via `manuscript.yaml`, Fiction/NonFiction catalog, chapter/appendix ordering, hierarchical references, and stable diagnostics.

Full contract: [PROTOCOL.md](PROTOCOL.md) (also packed under `docs/`).

## Install

```bash
dotnet add package Novolis.Manuscript.Protocol
```

Depends on YamlDotNet only. No Markdig, QuestPDF, or Avalonia.

## Quick start

```csharp
using Novolis.Manuscript.Protocol;

var workspace = ManuscriptWorkspace.Open(startPath);
var snapshot = workspace.Read();

var catalog = snapshot.Catalog;
var diagnostics = snapshot.Diagnostics;
```

Legacy `content/series` / `content/books` layouts belong in `Novolis.Markup.Manuscript.LegacyBooks`, not this package.
