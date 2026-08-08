using Novolis.Manuscript;
using Novolis.Manuscript.Metrics;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptAsciiTests
{
    [Test]
    public async Task Normalize_replaces_house_style_punctuation()
    {
        var input = "\uFEFFHe said\u2014\u201Cwait\u201D\u2026\u00A0ok\u200B.";
        var result = ManuscriptAscii.Normalize(input);
        await Assert.That(result.Text).IsEqualTo("He said-\"wait\"... ok.");
        await Assert.That(result.Replacements).IsGreaterThan(0);
        await Assert.That(result.HasRemainingNonAscii).IsFalse();
    }

    [Test]
    public async Task Scan_finds_non_ascii()
    {
        var issues = ManuscriptAscii.Scan("plain\u2014dash");
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Codepoint).IsEqualTo(0x2014);
    }
}

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
