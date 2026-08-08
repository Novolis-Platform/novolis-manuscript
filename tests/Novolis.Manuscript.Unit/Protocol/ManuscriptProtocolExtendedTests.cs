using ProtocolWorkspace = Novolis.Manuscript.Protocol.ManuscriptWorkspace;
using Novolis.Manuscript.Protocol;

namespace Novolis.Manuscript.Unit.Protocol;

public sealed class ManuscriptProtocolExtendedTests
{
    [Test]
    public async Task MissingChaptersDirectory_EmitsNMP009()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            var book = Path.Combine(root, "src", "Fiction", "u1", "b1");
            Directory.CreateDirectory(book);
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: B\n");
            // no Chapters/

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.MissingChaptersDirectory)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task MissingDocumentTitle_EmitsNMP012()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            var chapters = Path.Combine(root, "src", "Fiction", "u1", "b1", "Chapters");
            Directory.CreateDirectory(chapters);
            File.WriteAllText(Path.Combine(root, "src", "Fiction", "u1", "b1", "book.yaml"), "title: B\n");
            File.WriteAllText(Path.Combine(chapters, "1-no-heading.md"), "Just body text.\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.MissingDocumentTitle)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task MissingUniverseYaml_EmitsNMP004()
    {
        var root = CreateMinimalWorkspace(writeUniverse: false);
        try
        {
            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.MissingUniverseMetadata)).IsTrue();
            await Assert.That(snapshot.Catalog.Fiction.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task MissingSubjectYaml_EmitsNMP005()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            var subject = Path.Combine(root, "src", "NonFiction", "s1");
            Directory.CreateDirectory(subject);
            // no subject.yaml

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.MissingSubjectMetadata)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task InvalidIdentifier_EmitsNMP003()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            var bad = Path.Combine(root, "src", "Fiction", "Not_Valid");
            Directory.CreateDirectory(bad);
            File.WriteAllText(Path.Combine(bad, "universe.yaml"), "title: Bad\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.InvalidIdentifier)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task SeriesBookMissingOrder_EmitsNMP103()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            var series = Path.Combine(root, "src", "Fiction", "u1", "cycle");
            var book = Path.Combine(series, "book-a");
            Directory.CreateDirectory(Path.Combine(book, "Chapters"));
            File.WriteAllText(Path.Combine(series, "series.yaml"), "title: Cycle\n");
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: A\n"); // no order
            File.WriteAllText(Path.Combine(book, "Chapters", "1-a.md"), "# A\n\nBody.\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.SeriesBookMissingOrder)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task DuplicateSeriesOrder_EmitsNMP104()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            var series = Path.Combine(root, "src", "Fiction", "u1", "cycle");
            foreach (var id in new[] { "a", "b" })
            {
                var book = Path.Combine(series, id);
                Directory.CreateDirectory(Path.Combine(book, "Chapters"));
                File.WriteAllText(Path.Combine(book, "book.yaml"), $"title: {id}\norder: 1\n");
                File.WriteAllText(Path.Combine(book, "Chapters", "1-x.md"), $"# {id}\n\nBody.\n");
            }

            File.WriteAllText(Path.Combine(series, "series.yaml"), "title: Cycle\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.DuplicateSeriesOrder)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task EmptyBook_EmitsNMP101()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            var book = Path.Combine(root, "src", "Fiction", "u1", "empty-book");
            Directory.CreateDirectory(Path.Combine(book, "Chapters"));
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: Empty\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.EmptyBook)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task EmptyReferences_EmitsNMP102()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src", "Fiction", "u1", "References"));

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.EmptyReferenceFolder)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Appendices_OrderedIndependently()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            var book = Path.Combine(root, "src", "Fiction", "u1", "b1");
            Directory.CreateDirectory(Path.Combine(book, "Chapters"));
            Directory.CreateDirectory(Path.Combine(book, "Appendices"));
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: B\n");
            File.WriteAllText(Path.Combine(book, "Chapters", "1-ch.md"), "# Ch\n\nBody.\n");
            File.WriteAllText(Path.Combine(book, "Appendices", "5-notes.md"), "# Notes\n\nA.\n");
            File.WriteAllText(Path.Combine(book, "Appendices", "10-more.md"), "# More\n\nB.\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            var loaded = snapshot.Catalog.Fiction[0].Books[0];
            await Assert.That(loaded.Appendices.Count).IsEqualTo(2);
            await Assert.That(loaded.Appendices[0].Order).IsEqualTo(5);
            await Assert.That(loaded.Appendices[0].Kind).IsEqualTo(ManuscriptDocumentKind.Appendix);
            await Assert.That(loaded.Appendices[1].Slug).IsEqualTo("more");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task BookPublicationAndExtensions_RoundTrip()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            var book = Path.Combine(root, "src", "Fiction", "u1", "b1");
            Directory.CreateDirectory(Path.Combine(book, "Chapters"));
            File.WriteAllText(Path.Combine(book, "book.yaml"), """
                title: B
                publication:
                  version: 1.2.3
                  isbn: null
                  date: null
                extensions:
                  novolis.metrics:
                    count_dialogue: true
                """);
            File.WriteAllText(Path.Combine(book, "Chapters", "1-a.md"), "# A\n\nBody.\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            var meta = snapshot.Catalog.Fiction[0].Books[0].Metadata;
            await Assert.That(meta.Publication!.Version).IsEqualTo("1.2.3");
            await Assert.That(meta.Extensions).IsNotNull();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Open_InvalidProtocol_Throws()
    {
        var root = Path.Combine(Path.GetTempPath(), "nmp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "manuscript.yaml"), """
                protocol: other.protocol
                version: 1
                """);

            var threw = false;
            try
            {
                ProtocolWorkspace.Open(root);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            await Assert.That(threw).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Open_MissingMarker_Throws()
    {
        var root = Path.Combine(Path.GetTempPath(), "nmp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var threw = false;
            try
            {
                ProtocolWorkspace.Open(root);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            await Assert.That(threw).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task SeriesAndBookReferenceScopes_AreDistinct()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            var series = Path.Combine(root, "src", "Fiction", "u1", "cycle");
            var book = Path.Combine(series, "b1");
            Directory.CreateDirectory(Path.Combine(series, "References", "shared"));
            Directory.CreateDirectory(Path.Combine(book, "Chapters"));
            Directory.CreateDirectory(Path.Combine(book, "References", "shared"));
            File.WriteAllText(Path.Combine(series, "series.yaml"), "title: Cycle\n");
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: B\norder: 1\n");
            File.WriteAllText(Path.Combine(book, "Chapters", "1-a.md"), "# A\n\nBody.\n");
            File.WriteAllText(Path.Combine(series, "References", "shared", "note.md"), "# Series Note\n\nS.\n");
            File.WriteAllText(Path.Combine(book, "References", "shared", "note.md"), "# Book Note\n\nB.\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            var seriesNode = snapshot.Catalog.Fiction[0].Series[0];
            var bookNode = seriesNode.Books[0];
            await Assert.That(seriesNode.References[0].Id)
                .IsEqualTo("fiction/u1/cycle/reference/shared/note");
            await Assert.That(bookNode.References[0].Id)
                .IsEqualTo("fiction/u1/cycle/b1/reference/shared/note");
            await Assert.That(seriesNode.References[0].Title).IsEqualTo("Series Note");
            await Assert.That(bookNode.References[0].Title).IsEqualTo("Book Note");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Inheritance_NearestWins_SeriesOverWorkspace()
    {
        var root = CreateMinimalWorkspace();
        try
        {
            File.WriteAllText(Path.Combine(root, "manuscript.yaml"), """
                protocol: novolis.manuscript
                version: 1
                defaults:
                  authors:
                    - Workspace Author
                  language: en-US
                  rights: Workspace Rights
                """);

            var series = Path.Combine(root, "src", "Fiction", "u1", "cycle");
            var book = Path.Combine(series, "b1");
            Directory.CreateDirectory(Path.Combine(book, "Chapters"));
            File.WriteAllText(Path.Combine(root, "src", "Fiction", "u1", "universe.yaml"), """
                title: U
                defaults:
                  language: nb-NO
                """);
            File.WriteAllText(Path.Combine(series, "series.yaml"), """
                title: Cycle
                defaults:
                  authors:
                    - Series Author
                """);
            File.WriteAllText(Path.Combine(book, "book.yaml"), "title: B\norder: 1\n");
            File.WriteAllText(Path.Combine(book, "Chapters", "1-a.md"), "# A\n\nBody.\n");

            var meta = ProtocolWorkspace.Open(root).Read().Catalog.Fiction[0].Series[0].Books[0].Metadata;
            await Assert.That(meta.Authors![0]).IsEqualTo("Series Author");
            await Assert.That(meta.Language).IsEqualTo("nb-NO");
            await Assert.That(meta.Rights).IsEqualTo("Workspace Rights");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string CreateMinimalWorkspace(bool writeUniverse = true)
    {
        var root = Path.Combine(Path.GetTempPath(), "nmp-ext-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "manuscript.yaml"), """
            protocol: novolis.manuscript
            version: 1
            """);

        var universe = Path.Combine(root, "src", "Fiction", "u1");
        Directory.CreateDirectory(universe);
        if (writeUniverse)
            File.WriteAllText(Path.Combine(universe, "universe.yaml"), "title: Universe\n");

        return root;
    }
}
