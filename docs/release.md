# Release

This repository publishes with the org CalVer scheme (`2026.1.*`) via `merge.yml` to GitHub Packages when packages are packable.

See [release-policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/release-policy.md).

Published docs: [https://novolis-platform.github.io/.github/novolis-manuscript/](https://novolis-platform.github.io/.github/novolis-manuscript/)

## Packages

- `Novolis.Manuscript`
- `Novolis.Manuscript.Editorial`
- `Novolis.Manuscript.Export.Audio`
- `Novolis.Manuscript.Export.Markdown`
- `Novolis.Manuscript.Export.Pdf`
- `Novolis.Manuscript.IO`
- `Novolis.Manuscript.LegacyBooks`
- `Novolis.Manuscript.Metrics`
- `Novolis.Manuscript.Protocol`
- `Novolis.Manuscript.References`

## Consumers

The `novolis-manuscript` developer tool is released from
[`novolis-tools`](https://github.com/Novolis-Platform/novolis-tools). This
repository publishes the library packages listed above.

Restore from nuget.org + `https://nuget.pkg.github.com/Novolis-Platform/index.json` only.

Local multi-repo iteration: open `d:\novolis\Novolis.Platform.slnx` (ProjectReference mode) — do not add a local feed.
