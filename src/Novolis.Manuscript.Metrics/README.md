# Novolis.Manuscript.Metrics

Word-count and TODO/FIXME/TK metrics for NMP/1 manuscript workspaces.

```csharp
var results = ManuscriptMetrics.RunAll(@"D:\repos\books");
var one = ManuscriptMetrics.RunOne(@"D:\repos\books", "the-calypso-cycle", "calypso");
```

Outputs land under `out/<series>/<book>/metrics/` (and `out/metrics/overview.metrics.md` for RunAll).
