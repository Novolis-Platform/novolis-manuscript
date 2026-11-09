<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-manuscript/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-manuscript/) · [Source](https://github.com/Novolis-Platform/novolis-manuscript)
<!-- novolis-pkg-brand:end -->

# Novolis.Manuscript.Export.Audio

TTS and audiobook export for manuscript workspaces.

Default synthesizer: **`EdgeTtsSynthesizer`** (`Novolis.Audio.Voice.EdgeTts`, LGPL) — online Edge Read Aloud, no Azure credentials. Apps may also plug in Azure Speech via `ISynthesizer`.

## Install

```powershell
dotnet add package Novolis.Manuscript.Export.Audio
```

## Usage

```csharp
using var synth = new EdgeTtsSynthesizer();
var pipeline = new AudiobookPipeline(synth);
await pipeline.GenerateAsync(bookId, chapters, new VoiceSettings(), options);
```

