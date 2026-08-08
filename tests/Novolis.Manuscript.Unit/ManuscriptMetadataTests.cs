using Novolis.Manuscript;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptMetadataTests
{
    [Test]
    public async Task Parse_Callouts()
    {
        var text = """
            # Chapter 1 - Lunch

            > [!date] 2495.001
            > [!pov] Ryn

            Hello world here.
            """;
        var (meta, body, format) = ManuscriptMetadata.Parse(text);
        await Assert.That(format).IsEqualTo(ManuscriptMetadataFormat.Callout);
        await Assert.That(meta.Date).IsEqualTo("2495.001");
        await Assert.That(meta.Pov).IsEqualTo("Ryn");
        await Assert.That(ManuscriptMetadata.CountWords(text)).IsGreaterThanOrEqualTo(3);
        await Assert.That(ManuscriptMetadata.GetBodyForWordCount(text).Contains("Hello")).IsTrue();
        await Assert.That(body.Contains("Hello")).IsTrue();
    }
}
