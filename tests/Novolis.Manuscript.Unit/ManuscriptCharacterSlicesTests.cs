using Novolis.Manuscript;
using Novolis.Manuscript.Metrics;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptCharacterSlicesTests
{
    [Test]
    public async Task Build_aggregates_pov_and_cast()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-slices-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "001-prologue.md"),
                """
                # Chapter 1 - Prologue

                > [!pov] Ryn
                > [!characters] Ryn, Tess

                Body.
                """);
            await File.WriteAllTextAsync(Path.Combine(dir, "002-next.md"),
                """
                # Chapter 2 - Next

                > [!pov] Tess
                > [!characters] Tess / Ryn

                Body.
                """);

            var report = ManuscriptCharacterSlices.Build("demo", dir);
            await Assert.That(report.Chapters.Count).IsEqualTo(2);
            await Assert.That(report.MissingPov.Count).IsEqualTo(0);
            await Assert.That(report.Characters.ContainsKey("Ryn")).IsTrue();
            await Assert.That(report.Characters["Ryn"].Pov.Count).IsEqualTo(1);
            await Assert.That(report.Characters["Ryn"].Characters.Count).IsEqualTo(2);

            var md = report.ToMarkdown("Ryn");
            await Assert.That(md).Contains("## Ryn");
            await Assert.That(md).Contains("POV chapters: 1");
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }
}
