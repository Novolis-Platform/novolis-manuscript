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
}
