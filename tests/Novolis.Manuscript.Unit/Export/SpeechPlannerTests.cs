using Novolis.Manuscript.Export.Audio;

namespace Novolis.Manuscript.Unit;

public sealed class SpeechPlannerTests
{
    [Test]
    public async Task Create_Chunks_And_Pauses()
    {
        var md = """
            # Title

            > [!pov] A

            Hello there friend.

            ***

            Second scene text.
            """;
        var plan = SpeechPlanner.Create(md, new SpeechOptions
        {
            SceneBreakMs = 500,
            MaxChunkChars = 2800,
            Pronunciation = new Dictionary<string, string> { ["Hello"] = "Hullo" }
        });

        await Assert.That(plan.Segments.Count).IsGreaterThanOrEqualTo(3);
        await Assert.That(plan.Segments.Any(s => s.Kind == SpeechSegmentKind.Pause)).IsTrue();
        await Assert.That(plan.Segments.Any(s => s.Text != null && s.Text.Contains("Hullo"))).IsTrue();
        await Assert.That(plan.PlanHash.Length).IsEqualTo(64);
    }
}
