using Novolis.Manuscript;
using Novolis.Manuscript.Export.Pdf;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptCoverageGapTests
{
    [Test]
    public async Task Workspace_TryOpen_FromSeriesLayout()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ms-ws-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "content", "series", "demo"));
        try
        {
            var ok = ManuscriptWorkspace.TryOpen(Path.Combine(root, "content", "series", "demo"), out var ws);
            await Assert.That(ok).IsTrue();
            await Assert.That(ws!.ContentRoot).IsEqualTo(Path.GetFullPath(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Workspace_TryOpen_RejectsMissingDirectory()
    {
        var ok = ManuscriptWorkspace.TryOpen(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}"), out var ws);
        await Assert.That(ok).IsFalse();
        await Assert.That(ws).IsNull();
    }

    [Test]
    public async Task Metadata_CalloutFrontMatterAndWordBody()
    {
        var text = """
            # Chapter 1 - Opening

            > [!date] 2026-01-01
            > [!pov] Narrator

            Real body for counting.
            """;
        var (meta, _, format) = ManuscriptMetadata.Parse(text);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Callout);
        await Assert.That(meta.Date).IsEqualTo("2026-01-01");
        await Assert.That(meta.Pov).IsEqualTo("Narrator");
        await Assert.That(meta.Title).IsEqualTo("Opening");
        await Assert.That(ManuscriptMetadata.GetBodyForWordCount(text)).Contains("Real body");
    }

    [Test]
    public async Task PrintSettings_LoadFromJsonOverrides()
    {
        var path = Path.Combine(Path.GetTempPath(), $"print-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, """{"includeCover":false,"bodyFontSize":12}""");
            var settings = ManuscriptPrintSettings.Load(path);
            await Assert.That(settings.IncludeCover).IsFalse();
            await Assert.That(settings.BodyFontSize).IsEqualTo(12f);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task Metadata_ApplyCallout_aliases_and_extra()
    {
        var text = """
            # Chapter 1 - Tags

            > [!loc] Hangar
            > [!chars] Ryn
            > [!point_of_view] Tess
            > [!note] Remember
            > [!custom] ExtraValue
            > [!status] draft

            Body.
            """;
        var (meta, _, format) = ManuscriptMetadata.Parse(text);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Callout);
        await Assert.That(meta.Location).IsEqualTo("Hangar");
        await Assert.That(meta.Characters).IsEqualTo("Ryn");
        await Assert.That(meta.Pov).IsEqualTo("Tess");
        await Assert.That(meta.Notes).IsEqualTo("Remember");
        await Assert.That(meta.Status).IsEqualTo("draft");
        await Assert.That(meta.Extra["custom"]).IsEqualTo("ExtraValue");

        var yaml = """
            ---
            loc: Bridge
            chars: Kai
            notes: yaml-note
            mystery: x
            ---

            # Chapter 2 - Yaml

            Body.
            """;
        var (ymeta, _, yformat) = ManuscriptMetadata.Parse(yaml);
        await Assert.That(yformat).IsEqualTo(ManuscriptMetadataFormat.Yaml);
        await Assert.That(ymeta.Location).IsEqualTo("Bridge");
        await Assert.That(ymeta.Characters).IsEqualTo("Kai");
        await Assert.That(ymeta.Notes).IsEqualTo("yaml-note");
        await Assert.That(ymeta.Extra["mystery"]).IsEqualTo("x");
    }
}
