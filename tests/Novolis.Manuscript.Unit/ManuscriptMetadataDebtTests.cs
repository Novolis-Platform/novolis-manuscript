using Novolis.Manuscript;
using Novolis.Manuscript.Metrics;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptMetadataDebtTests
{
    [Test]
    public async Task Diagnose_flags_tk_and_missing_pov()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-meta-debt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "001.md"),
                """
                # Chapter 1 - Stub

                > [!date] TK
                > [!location] Calypso

                Body text.
                """);

            var findings = ManuscriptMetadataDebt.Diagnose(dir);
            await Assert.That(findings.Any(f => f.Code == MetadataDebtCodes.MetadataTk)).IsTrue();
            await Assert.That(findings.Any(f => f.Code == MetadataDebtCodes.MissingPov)).IsTrue();
            await Assert.That(findings.Any(f => f.Code == MetadataDebtCodes.MissingCharacters)).IsTrue();
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Test]
    public async Task DiagnoseMeta_skips_complete_metadata()
    {
        var meta = new ManuscriptChapterMetadata
        {
            Pov = "James",
            Characters = "James, Marsh",
            Location = "Bridge",
        };
        var findings = ManuscriptMetadataDebt.DiagnoseMeta(meta);
        await Assert.That(findings.Count).IsEqualTo(0);
    }
}
