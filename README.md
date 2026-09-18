<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start — embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Package README |
|---------|---------|----------------|
| `Novolis.Manuscript` | `dotnet add package Novolis.Manuscript` | [README](https://github.com/Novolis-Platform/novolis-manuscript/blob/main/src/Novolis.Manuscript/README.md) |
| `Novolis.Manuscript.Editorial` | `dotnet add package Novolis.Manuscript.Editorial` | [README](https://github.com/Novolis-Platform/novolis-manuscript/blob/main/src/Novolis.Manuscript.Editorial/README.md) |
| `Novolis.Manuscript.Export.Audio` | `dotnet add package Novolis.Manuscript.Export.Audio` | [README](https://github.com/Novolis-Platform/novolis-manuscript/blob/main/src/Novolis.Manuscript.Export.Audio/README.md) |
| `Novolis.Manuscript.Export.Markdown` | `dotnet add package Novolis.Manuscript.Export.Markdown` | [README](https://github.com/Novolis-Platform/novolis-manuscript/blob/main/src/Novolis.Manuscript.Export.Markdown/README.md) |
| `Novolis.Manuscript.Export.Pdf` | `dotnet add package Novolis.Manuscript.Export.Pdf` | [README](https://github.com/Novolis-Platform/novolis-manuscript/blob/main/src/Novolis.Manuscript.Export.Pdf/README.md) |
| `Novolis.Manuscript.IO` | `dotnet add package Novolis.Manuscript.IO` | [README](https://github.com/Novolis-Platform/novolis-manuscript/blob/main/src/Novolis.Manuscript.IO/README.md) |
| `Novolis.Manuscript.LegacyBooks` | `dotnet add package Novolis.Manuscript.LegacyBooks` | [README](https://github.com/Novolis-Platform/novolis-manuscript/blob/main/src/Novolis.Manuscript.LegacyBooks/README.md) |
| `Novolis.Manuscript.Metrics` | `dotnet add package Novolis.Manuscript.Metrics` | [README](https://github.com/Novolis-Platform/novolis-manuscript/blob/main/src/Novolis.Manuscript.Metrics/README.md) |
| `Novolis.Manuscript.Protocol` | `dotnet add package Novolis.Manuscript.Protocol` | [README](https://github.com/Novolis-Platform/novolis-manuscript/blob/main/src/Novolis.Manuscript.Protocol/README.md) |
| `Novolis.Manuscript.References` | `dotnet add package Novolis.Manuscript.References` | [README](https://github.com/Novolis-Platform/novolis-manuscript/blob/main/src/Novolis.Manuscript.References/README.md) |

For NuGet.org and Visual Studio, the **embedded** README.md inside each package is authoritative.

<!-- novolis-package-index:end -->

<!-- novolis-marketing:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-brand-transparent.svg" width="360" alt="Novolis"/>
  </a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/banners/novolis-manuscript.svg" width="100%" alt="novolis-manuscript"/>
</p>

<p align="center">
  <strong>Long-form manuscript tooling</strong><br/>
  Manuscript authoring helpers that sit beside Markup and Documents.
</p>

<p align="center">
  <a href="https://novolis-platform.github.io/.github/novolis-manuscript/"><img src="https://img.shields.io/badge/docs-portfolio-0a7ea3" alt="docs"/></a>
  <a href="https://github.com/Novolis-Platform/novolis-manuscript/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-manuscript/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-manuscript"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
  <a href="https://novolis-platform.github.io/.github/novolis-manuscript/">Docs</a>
  ·
  <a href="https://nuget.pkg.github.com/Novolis-Platform/index.json"><code>https://nuget.pkg.github.com/Novolis-Platform/index.json</code></a>
  ·
  <a href="https://github.com/Novolis-Platform/.github/blob/main/profile/README.md">Org landing</a>
  ·
  <a href="https://github.com/Novolis-Platform/novolis-governance">Governance</a>
</p>

---
<!-- novolis-marketing:end -->
# novolis-manuscript

Packable Manuscript framework for Books Writer Studio and Books Mobile.

## Packages

| PackageId | Role |
|-----------|------|
| `Novolis.Manuscript` | Workspace façade / doctor (no PDF, no audio) |
| `Novolis.Manuscript.Protocol` | NMP/1 reader |
| `Novolis.Manuscript.LegacyBooks` | Legacy `content/` adapter |
| `Novolis.Manuscript.IO` | Tree surgery, working copies, git/GitHub façades |
| `Novolis.Manuscript.Metrics` | Word counts, character slices, metadata TK debt |
| `Novolis.Manuscript.Editorial` | Lexicon, AI-slop patterns, naming variants |
| `Novolis.Manuscript.References` | Reference cards/sets on `Novolis.IO.Indexing` |
| `Novolis.Manuscript.Export.Pdf` | PDF export |
| `Novolis.Manuscript.Export.Audio` | TTS / audiobook |

Avalonia chrome for editors lives in `Novolis.Avalonia.Manuscript` (`novolis-avalonia`).

The `novolis-manuscript` developer tool is published from
[`novolis-tools`](https://github.com/Novolis-Platform/novolis-tools). This
repository publishes the manuscript libraries only.

## Build

```powershell
dotnet build d:\novolis\novolis-manuscript\Novolis.Manuscript.slnx -p:NovolisUseProjectReferences=true
dotnet test d:\novolis\novolis-manuscript\tests\Novolis.Manuscript.Unit\Novolis.Manuscript.Unit.csproj -p:NovolisUseProjectReferences=true
```

Cross-repo iteration: open `d:\novolis\Novolis.Platform.slnx` (ProjectReference mode).

