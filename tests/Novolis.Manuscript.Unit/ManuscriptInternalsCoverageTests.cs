using Novolis.Manuscript.Export.Pdf;
using Novolis.Manuscript.Protocol;
using Novolis.Manuscript.Protocol.Internal;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptInternalsCoverageTests
{
    [Test]
    public async Task ChapterMetadataDisplay_merges_and_skips_blanks()
    {
        var plain = ChapterMetadataDisplay.BuildPlainLines(
        [
            ("date", "2026-01-01"),
            ("time", "08:00"),
            ("pov", "Ryn"),
            ("empty", ""),
            ("time", "09:00"),
            ("date", "2026-01-02"),
        ], debugMode: false);
        await Assert.That(plain[0]).IsEqualTo("2026-01-01 08:00");
        await Assert.That(plain).Contains("Ryn");
        await Assert.That(plain).Contains("2026-01-02 09:00");

        var debug = ChapterMetadataDisplay.BuildPlainLines([("pov", "Tess")], debugMode: true);
        await Assert.That(debug[0]).IsEqualTo("POV  Tess");

        var html = ChapterMetadataDisplay.BuildReaderValueHtmlLines(
        [
            ("date", "<em>2026-01-01</em>"),
            ("time", "<em>08:00</em>"),
            ("pov", "<b></b>"),
            ("time", "09:00"),
            ("date", "2026-01-02"),
            ("loc", "Bridge"),
        ]);
        await Assert.That(html[0]).Contains("2026-01-01");
        await Assert.That(html).Contains("2026-01-02 09:00");
        await Assert.That(html).Contains("Bridge");
    }

    [Test]
    public async Task ProtocolMetadataReader_error_and_list_paths()
    {
        var reader = new ProtocolMetadataReader();
        var diagnostics = new List<ManuscriptDiagnostic>();
        var root = Path.Combine(Path.GetTempPath(), $"ms-int-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var missing = reader.ReadBook(Path.Combine(root, "missing.yaml"), diagnostics);
            await Assert.That(missing.Success).IsFalse();

            var bookPath = Path.Combine(root, "book.yaml");
            File.WriteAllText(bookPath, "order: 1\n");
            var noTitle = reader.ReadBook(bookPath, diagnostics);
            await Assert.That(noTitle.Success).IsFalse();

            File.WriteAllText(bookPath, "- just\n- a\n- list\n");
            var notMap = reader.ReadBook(bookPath, diagnostics);
            await Assert.That(notMap.Success).IsFalse();

            File.WriteAllText(bookPath, "\uFEFFtitle: BomBook\norder: 2\ntargets:\n  words: 100\n");
            var bom = reader.ReadBook(bookPath, diagnostics);
            await Assert.That(bom.Success).IsTrue();
            await Assert.That(bom.Value!.Targets!.Words).IsEqualTo(100);

            var chapterDiag = new List<ManuscriptDiagnostic>();
            var listFm = reader.ReadChapterFrontMatter("- a\n- b\n", "ch.md", chapterDiag);
            await Assert.That(listFm.Success).IsFalse();

            var emptyDoc = reader.ReadChapterFrontMatter("\n", "ch.md", chapterDiag);
            await Assert.That(emptyDoc.Success).IsTrue();

            var ok = reader.ReadChapterFrontMatter("tags: one, two\ncharacters:\n  - A\n  - B\n", "ch.md", chapterDiag);
            await Assert.That(ok.Success).IsTrue();
            await Assert.That(ok.Value!.Tags!.Count).IsEqualTo(2);
            await Assert.That(ok.Value.Characters!.Count).IsEqualTo(2);

            var refDiag = new List<ManuscriptDiagnostic>();
            var refList = reader.ReadReferenceFrontMatter("- x\n", "r.md", refDiag);
            await Assert.That(refList.Success).IsFalse();
            var refOk = reader.ReadReferenceFrontMatter("aliases:\n  - a\ntags:\n  - t\n", "r.md", refDiag);
            await Assert.That(refOk.Success).IsTrue();

            var entity = Path.Combine(root, "universe.yaml");
            File.WriteAllText(entity, "description: no title\n");
            var noEntityTitle = reader.ReadUniverse(entity, diagnostics);
            await Assert.That(noEntityTitle.Success).IsFalse();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task DocumentReader_split_and_h1_helpers()
    {
        var (fm, body) = DocumentReader.SplitFrontMatter("---\ntags: a\n---\n# Title\n\nBody\n");
        await Assert.That(fm).IsEqualTo("tags: a");
        await Assert.That(body).Contains("# Title");

        var (fm2, body2) = DocumentReader.SplitFrontMatter("---\nonly\n---");
        await Assert.That(fm2).IsEqualTo("only");
        await Assert.That(body2).IsEqualTo(string.Empty);

        var (fm3, body3) = DocumentReader.SplitFrontMatter("no front matter");
        await Assert.That(fm3).IsNull();
        await Assert.That(body3).IsEqualTo("no front matter");

        await Assert.That(DocumentReader.ReadFirstH1("\n<!-- c -->\n# Hello\n")).IsEqualTo("Hello");
        await Assert.That(DocumentReader.ReadFirstH1("plain\n")).IsNull();
    }
}
