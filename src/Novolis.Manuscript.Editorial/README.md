<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-manuscript">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Manuscript.Editorial

Deterministic editorial detectors for NMP manuscript chapter prose: lexicon forbid/prefer lists, AI-slop patterns, and naming variants.

Does **not** depend on MachineLearning or Avalonia. Protocol stays structural — these findings are separate from `ManuscriptDoctor`.

## Install

```bash
dotnet add package Novolis.Manuscript.Editorial
```

## Quick start

```csharp
using Novolis.Manuscript.Editorial;

var findings = EditorialAnalyzer.AnalyzeChaptersDir(chaptersDir, new EditorialOptions
{
    Profile = EditorialProfile.Fiction,
});

foreach (var f in findings)
    Console.WriteLine($"{f.Code}: {f.Message}");
```

Finding codes: `editorial-lexicon-forbid`, `editorial-lexicon-prefer`, `editorial-slop-*`, `editorial-naming-variant`.

