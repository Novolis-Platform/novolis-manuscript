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
