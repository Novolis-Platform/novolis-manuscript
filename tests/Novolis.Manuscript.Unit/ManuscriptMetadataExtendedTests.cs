using Novolis.Manuscript;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptMetadataExtendedTests
{
    [Test]
    public async Task Parse_none_format_with_heading_only()
    {
        var text = "# Chapter 10 - Solo\n\nOnly body.";
        var (meta, body, format) = ManuscriptMetadata.Parse(text);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Callout);
        await Assert.That(meta.Number).IsEqualTo("10");
        await Assert.That(meta.Title).IsEqualTo("Solo");
        await Assert.That(body).Contains("Only body.");
    }

    [Test]
    public async Task ApplyCallouts_replaces_existing_callouts()
    {
        var text = """
            # Chapter 1 - Start

            > [!date] old
            > [!pov] old pov

            Body.
            """;
        var updated = ManuscriptMetadata.ApplyCallouts(text, new ManuscriptChapterMetadata
        {
            Number = "1",
            Title = "Start",
            Date = "2026-03-01",
            Time = "12:00",
            System = "Sys",
            Location = "Loc",
            Pov = "New POV",
            Characters = "A,B",
            Status = "draft",
            Notes = "note",
            Extra = { ["custom"] = "x" },
        });
        await Assert.That(updated).Contains("> [!date] 2026-03-01");
        await Assert.That(updated).Contains("> [!custom] x");
        await Assert.That(updated).DoesNotContain("old pov");
    }

    [Test]
    public async Task Yaml_ignores_comments_and_blank_keys()
    {
        var text = """
            ---
            # comment
            date: 2026-04-01
            :
            ---
            # Chapter 5 - Yaml

            Words here.
            """;
        var (meta, _, format) = ManuscriptMetadata.Parse(text);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Yaml);
        await Assert.That(meta.Date).IsEqualTo("2026-04-01");
        await Assert.That(ManuscriptMetadata.CountWords(text)).IsGreaterThan(0);
        await Assert.That(ManuscriptMetadata.GetBodyForWordCount(text)).Contains("Words");
    }

    [Test]
    public async Task GetBodyForWordCount_strips_heading_and_callouts()
    {
        var text = """
            # Chapter 7 - Count

            > [!date] x

            alpha beta gamma.
            """;
        var body = ManuscriptMetadata.GetBodyForWordCount(text);
        await Assert.That(body).Contains("alpha beta gamma");
        await Assert.That(body).DoesNotContain("[!date]");
    }
}
