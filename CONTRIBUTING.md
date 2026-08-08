# Contributing

See [novolis-governance](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/contribution-policy.md).

Maintainers and agents: commit on `main` and push (do not open ordinary PRs). External fork PRs use `pull-request.yml`.

Packable libraries must follow [documentation-policy.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/documentation-policy.md) (XML API docs and package READMEs).

## Manuscript layering

- **Libraries** own ascii, metrics, slices, surgery, doctor, editorial, and export pipelines ([library-vs-cli](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-vs-cli.md)).
- **`Novolis.Manuscript.Cli`** is argv → library only (exit codes / JSON).
- **`Novolis.Avalonia.Manuscript`** is composable chrome panels — not a product host ([Avalonia grain](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/avalonia-composition-grain.md)).

