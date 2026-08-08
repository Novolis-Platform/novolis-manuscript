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
  <strong>Manuscript app framework</strong><br/>
  Protocol, legacy books adapter, workspace doctor, IO, PDF/audio export, and Avalonia chrome.
</p>

<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-manuscript/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-manuscript/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-manuscript"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
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
| `Novolis.Manuscript.Export.Pdf` | PDF export |
| `Novolis.Manuscript.Export.Audio` | TTS / audiobook |
| `Novolis.Manuscript.Cli` | `novolis-manuscript` tool |
| `Novolis.Avalonia.Manuscript` | Shared editor chrome |

## Build

```powershell
dotnet build d:\novolis\novolis-manuscript\Novolis.Manuscript.slnx -p:NovolisUseProjectReferences=true
dotnet test d:\novolis\novolis-manuscript\tests\Novolis.Manuscript.Unit\Novolis.Manuscript.Unit.csproj -p:NovolisUseProjectReferences=true
```

Cross-repo iteration: open `d:\novolis\Novolis.Platform.slnx` (ProjectReference mode).
