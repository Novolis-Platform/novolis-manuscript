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

    [Test]
    public async Task Create_Splits_Blank_Line_Paragraphs()
    {
        var md = """
            # Title

            First paragraph here.

            Second paragraph here.
            """;
        var plan = SpeechPlanner.Create(md, new SpeechOptions { MaxChunkChars = 2800 }, speakTitle: false);
        var spoken = plan.Segments.Where(s => s.Kind == SpeechSegmentKind.Text).Select(s => s.Text!).ToList();
        await Assert.That(spoken.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(spoken[0]).Contains("First paragraph");
        await Assert.That(spoken[1]).Contains("Second paragraph");
    }
}
