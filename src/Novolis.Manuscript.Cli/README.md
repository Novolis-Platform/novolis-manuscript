# Novolis.Manuscript.Cli

Command-line tool for NMP/1 manuscript workspaces: chapter surgery, audiobook generation, print, and metrics.

```powershell
dotnet tool install -g Novolis.Manuscript.Cli --add-source https://nuget.pkg.github.com/Novolis-Platform/index.json

novolis-manuscript book list-books --workspace D:\repos\books
novolis-manuscript book doctor --series the-calypso-cycle --book calypso
novolis-manuscript audio --series the-calypso-cycle --book calypso --dry-run
novolis-manuscript print --series the-calypso-cycle --book calypso
novolis-manuscript metrics --workspace D:\repos\books
```

Local ProjectRef:

```powershell
dotnet run --project d:\novolis\novolis-manuscript\src\Novolis.Manuscript.Cli\Novolis.Manuscript.Cli.csproj -p:NovolisUseProjectReferences=true -- book list-books --workspace D:\repos\books
```
