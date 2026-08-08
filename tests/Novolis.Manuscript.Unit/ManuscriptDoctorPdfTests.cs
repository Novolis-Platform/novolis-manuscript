using Novolis.Manuscript;
using Novolis.Manuscript.Export.Pdf;

namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptDoctorPdfTests
{
    [Test]
    public async Task Doctor_and_pdf_exporter()
    {
        var root = Directory.CreateTempSubdirectory("ms-doc-").FullName;
        try
        {
            var bookDir = Path.Combine(root, "content", "books", "demo");
            Directory.CreateDirectory(Path.Combine(bookDir, "chapters"));
            File.WriteAllText(Path.Combine(bookDir, "book.yaml"), "title: Demo\n");
            File.WriteAllText(Path.Combine(bookDir, "chapters", "01-one.md"), "# Chapter 1 - One\n\nHello.\n");
            var findings = ManuscriptDoctor.Diagnose(root);
            await Assert.That(findings).IsNotNull();
            var book = new ManuscriptCatalog().LoadStandaloneBooks(root).Single();
            var pdfPath = Path.Combine(root, "out.pdf");
            ManuscriptBookPdfExporter.ExportBook(book, pdfPath);
            await Assert.That(File.Exists(pdfPath)).IsTrue();
            await Assert.That(ManuscriptWorkspace.TryOpen(root, out var ws)).IsTrue();
            await Assert.That(ws).IsNotNull();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
