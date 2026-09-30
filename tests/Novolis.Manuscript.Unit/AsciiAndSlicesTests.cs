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
