# Novolis.Manuscript.Metrics

Word-count / TODO metrics and character-slice reports for NMP/1 manuscript workspaces.

```csharp
var results = ManuscriptMetrics.RunAll(@"D:\repos\books");
var one = ManuscriptMetrics.RunOne(@"D:\repos\books", "the-calypso-cycle", "calypso");
var slices = ManuscriptCharacterSlices.Build("calypso", chaptersDir);
Console.Write(slices.ToMarkdown());
var debt = ManuscriptMetadataDebt.Diagnose(chaptersDir);
```

Metrics outputs land under `out/<series>/<book>/metrics/` (and `out/metrics/overview.metrics.md` for RunAll).
Metadata TK / missing pov-characters findings use codes `metadata-tk`, `metadata-missing-pov`, `metadata-missing-characters`.
