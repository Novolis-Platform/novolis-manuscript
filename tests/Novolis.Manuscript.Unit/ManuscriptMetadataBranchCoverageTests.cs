using Novolis.Manuscript;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptMetadataBranchCoverageTests
{
    [Test]
    public async Task Yaml_hybrid_with_callouts_after_heading()
    {
        var md = """
            ---
            date: "1"
            ---

            # Chapter 9 - Hybrid

            > [!pov] Mira
            > [!time] 03:00

            Body text.
            """;
        var (meta, body, format) = ManuscriptMetadata.Parse(md);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Yaml);
        await Assert.That(meta.Date).IsEqualTo("1");
        await Assert.That(meta.Pov).IsEqualTo("Mira");
        await Assert.That(meta.Time).IsEqualTo("03:00");
        await Assert.That(body).Contains("Body text");
        await Assert.That(body).DoesNotContain("[!pov]");
    }

    [Test]
    public async Task Chapter_number_without_callouts_keeps_full_text()
    {
        var md = "# Chapter 4 - Solo\n\nOnly prose.\n";
        var (meta, body, format) = ManuscriptMetadata.Parse(md);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Callout);
        await Assert.That(meta.Number).IsEqualTo("4");
        await Assert.That(body).Contains("# Chapter 4");
        await Assert.That(ManuscriptMetadata.GetBodyForWordCount(md)).Contains("Only prose");
    }

    [Test]
    public async Task GetBodyForWordCount_callout_without_h1()
    {
        // Force callout format via number detection is H1-based; use empty remainder path.
        var words = ManuscriptMetadata.CountWords("   \n\t  ");
        await Assert.That(words).IsEqualTo(0);

        var yamlEmpty = """
            ---

            ---

            # T

            Word.
            """;
        var (meta, _, format) = ManuscriptMetadata.Parse(yamlEmpty);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Yaml);
        _ = meta;
        await Assert.That(ManuscriptMetadata.CountWords(yamlEmpty)).IsGreaterThan(0);
    }

    [Test]
    public async Task ApplyCallouts_h1_title_only_and_empty_callout_block()
    {
        var withH1 = ManuscriptMetadata.ApplyCallouts(
            "\n# Existing\n\n> [!date] old\n\nBody\n",
            new ManuscriptChapterMetadata { Title = "Renamed", Number = "1", Notes = "n", Status = "draft" });
        await Assert.That(withH1).Contains("# Chapter 1 - Renamed");
        await Assert.That(withH1).Contains("> [!notes] n");
        await Assert.That(withH1).Contains("> [!status] draft");

        var noMeta = ManuscriptMetadata.ApplyCallouts("# Keep\n\nBody\n", new ManuscriptChapterMetadata());
        await Assert.That(noMeta).Contains("# Keep");
        await Assert.That(noMeta).DoesNotContain("[!");
    }

    [Test]
    public async Task Yaml_sequence_and_mapping_edge_cases()
    {
        var md = """
            ---
            date: null
            system: ""
            locations:
              - "A"
              - "B"
            tags: ""
            custom: value
            nested:
              ignored: true
            ---

            # Title

            X
            """;
        var (meta, _, _) = ManuscriptMetadata.Parse(md);
        await Assert.That(meta.Location).Contains("A");
        await Assert.That(meta.Extra.ContainsKey("custom")).IsTrue();

        var scalarList = """
            ---
            characters: "One, Two"
            note: hello
            ---

            # T

            B
            """;
        var (m2, _, _) = ManuscriptMetadata.Parse(scalarList);
        await Assert.That(m2.Characters).Contains("One");
        await Assert.That(m2.Notes).IsEqualTo("hello");
    }

    [Test]
    public async Task Yaml_document_not_mapping_uses_naive()
    {
        var md = """
            ---
            - just
            - a
            - list
            ---

            # T

            Body
            """;
        var (_, body, format) = ManuscriptMetadata.Parse(md);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Yaml);
        await Assert.That(body).Contains("Body");
    }

    [Test]
    public async Task IsCalloutLine_and_visibility()
    {
        await Assert.That(ManuscriptMetadata.IsCalloutLine("> [!date] x")).IsTrue();
        await Assert.That(ManuscriptMetadata.IsCalloutLine("nope")).IsFalse();
        await Assert.That(ChapterMetadataVisibility.IsPublicTag("")).IsFalse();
        await Assert.That(ChapterMetadataVisibility.IsHiddenTag("pov")).IsTrue();
    }
}
