<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-manuscript/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-manuscript/) · [Source](https://github.com/Novolis-Platform/novolis-manuscript)
<!-- novolis-pkg-brand:end -->

# Novolis.Manuscript.Metrics

Word-count / TODO metrics and character-slice reports for NMP/1 manuscript workspaces.

## Install

```powershell
dotnet add package Novolis.Manuscript.Metrics
```

## Usage

```csharp
var results = ManuscriptMetrics.RunAll(@"D:\repos\books");
var one = ManuscriptMetrics.RunOne(@"D:\repos\books", "the-calypso-cycle", "calypso");
var slices = ManuscriptCharacterSlices.Build("calypso", chaptersDir);
Console.Write(slices.ToMarkdown());
var debt = ManuscriptMetadataDebt.Diagnose(chaptersDir);
```

Metrics outputs land under `out/<series>/<book>/metrics/` (and `out/metrics/overview.metrics.md` for RunAll).
Metadata TK / missing pov-characters findings use codes `metadata-tk`, `metadata-missing-pov`, `metadata-missing-characters`.

