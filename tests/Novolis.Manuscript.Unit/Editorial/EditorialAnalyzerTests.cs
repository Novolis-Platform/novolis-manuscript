using Novolis.Manuscript;
using Novolis.Manuscript.Editorial;

namespace Novolis.Manuscript.Unit.Editorial;

public sealed class EditorialAnalyzerTests
{
    [Test]
    public async Task AnalyzeText_strips_metadata_and_runs_rules()
    {
        var md = """
            # Chapter 1 - Test

            > [!pov] James
            > [!characters] James, Marsh

            Not a collision. A controlled strike.
            They used a phaser.
            """;
        var findings = EditorialAnalyzer.AnalyzeText(md, EditorialProfiles.Calypso());
        await Assert.That(findings.Any(f => f.Code == EditorialCodes.SlopCorrelativeNegation)).IsTrue();
        await Assert.That(findings.Any(f => f.Code == EditorialCodes.LexiconForbid)).IsTrue();
    }

    [Test]
    public async Task Neutral_fiction_skips_calypso_lexicon()
    {
        var findings = EditorialAnalyzer.AnalyzeText(
            "They engaged warp drive.",
            new EditorialOptions
            {
                Profile = EditorialProfile.Fiction,
                EnableLexicon = false,
                EnableSlop = false,
                EnableNaming = false,
            });
        await Assert.That(findings.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Nonfiction_profile_skips_lexicon_by_default()
    {
        var findings = EditorialAnalyzer.AnalyzeText(
            "They engaged warp drive.",
            new EditorialOptions { Profile = EditorialProfile.Nonfiction, EnableSlop = false, EnableNaming = false });
        await Assert.That(findings.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AnalyzeChaptersDir_scans_files()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-editorial-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "001.md"),
                "# Chapter 1 - A\n\nSomething shifted.\n");
            var findings = EditorialAnalyzer.AnalyzeChaptersDir(dir);
            await Assert.That(findings.Any(f => f.Code == EditorialCodes.SlopUnearnedProfundity)).IsTrue();
            await Assert.That(findings[0].Path).IsNotNull();
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }
}
