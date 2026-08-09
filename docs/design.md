# Design

Manuscript authoring helpers that sit beside Markup and Documents.

Published docs: [https://novolis-platform.github.io/.github/novolis-manuscript/](https://novolis-platform.github.io/.github/novolis-manuscript/)

## Layer placement

Documents/Markup island — Avalonia hosts may call PDF/HTML helpers; do not pull Avalonia into these packages.

## Goals

- Keep public APIs documented and packable as `Novolis.*` on GitHub Packages (when applicable).
- Prefer BCL types and existing Novolis packages over parallel abstractions.
- Document restore and ProjectReference-mode builds without local NuGet folder feeds.

## Non-goals

- Local NuGet folder feeds or committed cross-repo `ProjectReference` into sibling checkouts.
- Avalonia package references outside `Novolis.Avalonia.*`.
- Upward spine dependencies (e.g. Math → Simulation).

## Packages

- `Novolis.Manuscript`
- `Novolis.Manuscript.Cli`
- `Novolis.Manuscript.Editorial`
- `Novolis.Manuscript.Export.Audio`
- `Novolis.Manuscript.Export.Markdown`
- `Novolis.Manuscript.Export.Pdf`
- `Novolis.Manuscript.IO`
- `Novolis.Manuscript.LegacyBooks`
- `Novolis.Manuscript.Metrics`
- `Novolis.Manuscript.Protocol`
- `Novolis.Manuscript.References`

## Topics

- `dotnet`
- `manuscript`
- `markup`
- `novolis`
