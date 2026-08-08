using Novolis.Manuscript.IO;

namespace Novolis.Manuscript.Unit;

public sealed class LegacyChapterSurgeryTests
{
    [Test]
    public async Task InsertBetween_dry_run_does_not_write()
    {
        var temp = Directory.CreateTempSubdirectory("ms-io-");
        try
        {
            var chapters = Path.Combine(temp.FullName, "chapters");
            Directory.CreateDirectory(chapters);
            File.WriteAllText(Path.Combine(chapters, "01-one.md"), "# Chapter 1 - One\n\n");
            File.WriteAllText(Path.Combine(chapters, "02-two.md"), "# Chapter 2 - Two\n\n");
            var result = LegacyChapterSurgery.InsertBetween(chapters, 1.5, "Interlude", apply: false);
            await Assert.That(result.Applied).IsFalse();
            await Assert.That(Directory.GetFiles(chapters, "*.md").Length).IsEqualTo(2);
        }
        finally
        {
            temp.Delete(true);
        }
    }
}