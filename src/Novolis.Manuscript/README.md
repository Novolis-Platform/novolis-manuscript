<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-manuscript/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-manuscript/) · [Source](https://github.com/Novolis-Platform/novolis-manuscript)
<!-- novolis-pkg-brand:end -->

# Novolis.Manuscript

Chapter metadata, content catalog, and structural diagnostics for manuscript workspaces. Used by Books Mobile and Books Writer Studio.

PDF and audio export live in `Novolis.Manuscript.Export.Pdf` and `Novolis.Manuscript.Export.Audio`.

## Install

```bash
dotnet add package Novolis.Manuscript
```

Depends on `Novolis.IO.Paths` and `YamlDotNet`.

## Quick start

```csharp
using Novolis.Manuscript;

if (!ManuscriptWorkspace.TryOpen(startDir, out var workspace) || workspace is null)
    throw new InvalidOperationException("No manuscript workspace found.");

var series = workspace.Catalog.Load(workspace.ContentRoot);
var issues = ManuscriptDoctor.Diagnose(workspace.ContentRoot);
var (meta, body, format) = ManuscriptMetadata.Parse(chapterMarkdown);
```