using Novolis.Manuscript;
using Novolis.Manuscript.Editorial;

namespace Novolis.Manuscript.Unit.Editorial;

public sealed class LexiconRulesTests
{
    [Test]
    public async Task Forbid_flags_warp_and_hyperspace()
    {
        var findings = LexiconRules.Scan(
            "They engaged the warp drive into hyperspace.",
            forbiddenPhrases: EditorialProfiles.CalypsoForbiddenPhrases);
        await Assert.That(findings.Any(f => f.Code == EditorialCodes.LexiconForbid && f.Message.Contains("warp drive"))).IsTrue();
        await Assert.That(findings.Any(f => f.Code == EditorialCodes.LexiconForbid && f.Message.Contains("hyperspace"))).IsTrue();
    }

    [Test]
    public async Task Forbid_allows_warped_homonym()
    {
        var findings = LexiconRules.Scan(
            "The warped conduit hissed.",
            forbiddenPhrases: EditorialProfiles.CalypsoForbiddenPhrases);
        await Assert.That(findings.Any(f => f.Code == EditorialCodes.LexiconForbid)).IsFalse();
    }

    [Test]
    public async Task Prefer_flags_hallway()
    {
        var findings = LexiconRules.Scan(
            "He walked the hallway aft.",
            preferPairs: EditorialProfiles.CalypsoPreferPairs);
        await Assert.That(findings.Any(f => f.Code == EditorialCodes.LexiconPrefer)).IsTrue();
    }
}

public sealed class SlopPatternRulesTests
{
    [Test]
    public async Task Correlative_negation_fires()
    {
        var text = "Not a collision. A controlled strike.\n";
        var findings = SlopPatternRules.Scan(text);
        await Assert.That(findings.Any(f => f.Code == EditorialCodes.SlopCorrelativeNegation)).IsTrue();
    }

    [Test]
    public async Task Answer_as_question_fires()
    {
        var findings = SlopPatternRules.Scan("The result? More delays.");
        await Assert.That(findings.Any(f => f.Code == EditorialCodes.SlopAnswerAsQuestion)).IsTrue();
    }

    [Test]
    public async Task Clean_prose_is_quiet()
    {
        var text = "James checked the jump drive status on the bridge display.\nMarsh answered from engineering.";
        var findings = SlopPatternRules.Scan(text);
        await Assert.That(findings.Count).IsEqualTo(0);
    }
}

public sealed class NamingRulesTests
{
    [Test]
    public async Task Variant_spelling_flags_canonical()
    {
        var findings = NamingRules.Scan("Marshe spoke from the chair.", EditorialProfiles.CalypsoNames);
        await Assert.That(findings.Count).IsEqualTo(1);
        await Assert.That(findings[0].Code).IsEqualTo(EditorialCodes.NamingVariant);
        await Assert.That(findings[0].Message).Contains("Marsh");
    }

    [Test]
    public async Task Extra_names_merge()
    {
        var findings = NamingRules.Scan(
            "Torric waited.",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Torric"] = "Torrik" });
        await Assert.That(findings.Any(f => f.Message.Contains("Torrik"))).IsTrue();
    }
}

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
