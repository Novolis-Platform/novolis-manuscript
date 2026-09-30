using Novolis.Manuscript;
using Novolis.Manuscript.Export.Pdf;

namespace Novolis.Manuscript.Unit;

public sealed class BookPdfWriterTests
{
    [Test]
    public async Task Write_combine_is_default_and_includes_contents_outline()
    {
        var root = CreateTwoChapterBook();
        var outDir = Path.Combine(Path.GetTempPath(), $"book-pdf-combine-{Guid.NewGuid():N}");
        try
        {
            var bookDir = Path.Combine(root, "content", "series", "demo", "books", "book-one");
            var document = BookDocument.Open(bookDir);
            var paths = BookPdfWriter.Write(document, outDir, "book-one");

            await Assert.That(paths.CombinedPdfPath).IsNotNull();
            await Assert.That(File.Exists(paths.CombinedPdfPath!)).IsTrue();
            await Assert.That(Directory.Exists(Path.Combine(outDir, "book-one-chapters"))).IsFalse();
            await Assert.That(paths.ChaptersDirectory).IsNull();

            var pdf = System.Text.Encoding.Latin1.GetString(await File.ReadAllBytesAsync(paths.CombinedPdfPath!));
            await Assert.That(pdf).Contains("/Type /Outlines");
            await Assert.That(pdf).DoesNotContain(Utf16Title("Contents"));
            await Assert.That(pdf).Contains(Utf16Title("Opening"));
            await Assert.That(pdf).Contains(Utf16Title("The Watch"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            if (Directory.Exists(outDir))
                Directory.Delete(outDir, recursive: true);
        }
    }

    [Test]
    public async Task Write_chapters_does_not_write_combined_pdf()
    {
        var root = CreateTwoChapterBook();
        var outDir = Path.Combine(Path.GetTempPath(), $"book-pdf-ch-{Guid.NewGuid():N}");
        try
        {
            var bookDir = Path.Combine(root, "content", "series", "demo", "books", "book-one");
            var document = BookDocument.Open(bookDir);
            var paths = BookPdfWriter.Write(document, outDir, "book-one", BookPdfOutput.Chapters);

            await Assert.That(paths.CombinedPdfPath).IsNull();
            await Assert.That(File.Exists(Path.Combine(outDir, "book-one.pdf"))).IsFalse();
            await Assert.That(paths.ChaptersDirectory).IsNotNull();
            await Assert.That(Directory.Exists(paths.ChaptersDirectory!)).IsTrue();
            await Assert.That(paths.ChapterPdfPaths.Count).IsEqualTo(2);
            await Assert.That(File.Exists(Path.Combine(paths.ChaptersDirectory!, "001-opening.pdf"))).IsTrue();
            await Assert.That(File.Exists(Path.Combine(paths.ChaptersDirectory!, "002-watch.pdf"))).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            if (Directory.Exists(outDir))
                Directory.Delete(outDir, recursive: true);
        }
    }

    [Test]
    public async Task Write_both_writes_combined_and_chapter_folder()
    {
        var root = CreateTwoChapterBook();
        var outDir = Path.Combine(Path.GetTempPath(), $"book-pdf-both-{Guid.NewGuid():N}");
        try
        {
            var bookDir = Path.Combine(root, "content", "series", "demo", "books", "book-one");
            var document = BookDocument.Open(bookDir);
            var paths = BookPdfWriter.Write(document, outDir, "book-one", BookPdfOutput.Both);

            await Assert.That(File.Exists(paths.CombinedPdfPath!)).IsTrue();
            await Assert.That(paths.ChapterPdfPaths.Count).IsEqualTo(2);
            await Assert.That(File.Exists(Path.Combine(outDir, "book-one-chapters", "001-opening.pdf"))).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            if (Directory.Exists(outDir))
                Directory.Delete(outDir, recursive: true);
        }
    }

    [Test]
    public async Task ExportBookFolder_chapters_skips_companion_files()
    {
        var root = CreateTwoChapterBook();
        var outDir = Path.Combine(Path.GetTempPath(), $"book-pdf-folder-ch-{Guid.NewGuid():N}");
        try
        {
            var bookDir = Path.Combine(root, "content", "series", "demo", "books", "book-one");
            var paths = BookPrintExporter.ExportBookFolder(
                bookDir,
                outDir,
                "demo",
                "book-one",
                new BookPrintOptions { PdfOutput = BookPdfOutput.Chapters });

            await Assert.That(File.Exists(Path.Combine(outDir, "book-one.pdf"))).IsFalse();
            await Assert.That(File.Exists(Path.Combine(outDir, "book-one.md"))).IsFalse();
            await Assert.That(File.Exists(Path.Combine(outDir, "book-one.html"))).IsFalse();
            await Assert.That(File.Exists(Path.Combine(outDir, "book-one.txt"))).IsFalse();
            await Assert.That(Directory.Exists(paths.PdfPath)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            if (Directory.Exists(outDir))
                Directory.Delete(outDir, recursive: true);
        }
    }

    static string Utf16Title(string text)
    {
        var encoded = System.Text.Encoding.BigEndianUnicode.GetBytes(text);
        return "<FEFF" + Convert.ToHexString(encoded) + ">";
    }

    static string CreateTwoChapterBook()
    {
        var root = Path.Combine(Path.GetTempPath(), $"book-pdf-{Guid.NewGuid():N}");
        var series = Path.Combine(root, "content", "series", "demo");
        var book = Path.Combine(series, "books", "book-one");
        var chapters = Path.Combine(book, "chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(series, "series.yaml"), "id: demo\nname: Demo Series\n");
        File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Book One\nauthor: Auth\n");
        File.WriteAllText(Path.Combine(chapters, "001-opening.md"), "# Opening\n\nFirst chapter.\n");
        File.WriteAllText(Path.Combine(chapters, "002-watch.md"), "# The Watch\n\nSecond chapter.\n");
        return root;
    }
}
