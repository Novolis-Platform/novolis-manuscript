using Novolis.Manuscript;
using Novolis.Manuscript.Editorial;

namespace Novolis.Manuscript.Unit.Editorial;

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
