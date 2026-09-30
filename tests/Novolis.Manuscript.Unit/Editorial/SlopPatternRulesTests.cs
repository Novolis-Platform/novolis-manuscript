using Novolis.Manuscript;
using Novolis.Manuscript.Editorial;

namespace Novolis.Manuscript.Unit.Editorial;

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
