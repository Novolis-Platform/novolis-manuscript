using ProtocolWorkspace = Novolis.Manuscript.Protocol.ManuscriptWorkspace;
using Novolis.Manuscript.Protocol;

namespace Novolis.Manuscript.Unit.Protocol;

public sealed class ManuscriptProtocolTests
{
    [Test]
    public async Task Open_And_Read_CanonicalTree()
    {
        var root = CreateNmpFixture();
        try
        {
            var nested = Path.Combine(root, "src", "Fiction", "galactic-confederation", "the-calypso-cycle", "calypso");
            var workspace = ProtocolWorkspace.Open(nested);
            var snapshot = workspace.Read();

            await Assert.That(snapshot.Catalog.Fiction.Count).IsEqualTo(1);
            var universe = snapshot.Catalog.Fiction[0];
            await Assert.That(universe.Id).IsEqualTo("galactic-confederation");
            await Assert.That(universe.Series.Count).IsEqualTo(1);
            await Assert.That(universe.Series[0].Books.Count).IsEqualTo(1);

            var book = universe.Series[0].Books[0];
            await Assert.That(book.Address.BookId).IsEqualTo("calypso");
            await Assert.That(book.Address.SeriesId).IsEqualTo("the-calypso-cycle");
            await Assert.That(book.Chapters.Count).IsEqualTo(3);
            await Assert.That(book.Chapters[0].Order).IsEqualTo(10);
            await Assert.That(book.Chapters[0].Slug).IsEqualTo("the-rescue");
            await Assert.That(book.Chapters[0].Title).IsEqualTo("The Rescue");
            await Assert.That(book.Chapters[0].Metadata.Pov).IsEqualTo("Marsh");
            await Assert.That(book.Chapters[0].Metadata.Locations!.Count).IsEqualTo(2);

            await Assert.That(book.Metadata.Authors!.Count).IsEqualTo(1);
            await Assert.That(book.Metadata.Authors[0]).IsEqualTo("Frank R. Haugen");
            await Assert.That(book.Metadata.Language).IsEqualTo("en-US");

            await Assert.That(universe.References.Count).IsGreaterThanOrEqualTo(1);
            await Assert.That(universe.References[0].Id)
                .IsEqualTo("fiction/galactic-confederation/reference/history/timeline");

            await Assert.That(snapshot.Catalog.NonFiction.Count).IsEqualTo(1);
            await Assert.That(snapshot.Catalog.NonFiction[0].Books.Count).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Inheritance_BookAuthorsReplaceDefaults()
    {
        var root = CreateNmpFixture();
        try
        {
            var bookYaml = Path.Combine(
                root, "src", "Fiction", "galactic-confederation", "the-calypso-cycle", "calypso", "book.yaml");
            File.WriteAllText(bookYaml, """
                title: Calypso
                order: 1
                authors:
                  - Replacement Author
                """);

            var snapshot = ProtocolWorkspace.Open(root).Read();
            var book = snapshot.Catalog.Fiction[0].Series[0].Books[0];
            await Assert.That(book.Metadata.Authors!.Count).IsEqualTo(1);
            await Assert.That(book.Metadata.Authors[0]).IsEqualTo("Replacement Author");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task UnknownMetadataField_EmitsNMP014()
    {
        var root = CreateNmpFixture();
        try
        {
            var bookYaml = Path.Combine(
                root, "src", "Fiction", "galactic-confederation", "the-calypso-cycle", "calypso", "book.yaml");
            File.WriteAllText(bookYaml, """
                title: Calypso
                order: 1
                debug_mode: true
                """);

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.UnknownMetadataField
                && d.Message.Contains("debug_mode", StringComparison.Ordinal))).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task DuplicateDocumentOrder_EmitsNMP010()
    {
        var root = CreateNmpFixture();
        try
        {
            var chapters = Path.Combine(
                root, "src", "Fiction", "galactic-confederation", "the-calypso-cycle", "calypso", "Chapters");
            File.WriteAllText(Path.Combine(chapters, "10-duplicate.md"), "# Duplicate\n\nBody.\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.DuplicateDocumentOrder)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task InvalidDocumentFilename_EmitsNMP011()
    {
        var root = CreateNmpFixture();
        try
        {
            var chapters = Path.Combine(
                root, "src", "Fiction", "galactic-confederation", "the-calypso-cycle", "calypso", "Chapters");
            File.WriteAllText(Path.Combine(chapters, "bad_name.md"), "# Bad\n\nBody.\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Diagnostics.Any(d =>
                d.Code == ManuscriptDiagnosticCodes.InvalidDocumentFilename)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Open_UnsupportedVersion_Throws()
    {
        var root = Path.Combine(Path.GetTempPath(), "nmp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "manuscript.yaml"), """
                protocol: novolis.manuscript
                version: 99
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
    public async Task StandaloneFictionBook_HasNullSeriesId()
    {
        var root = CreateNmpFixture();
        try
        {
            var standalone = Path.Combine(root, "src", "Fiction", "galactic-confederation", "lone-novel");
            Directory.CreateDirectory(Path.Combine(standalone, "Chapters"));
            File.WriteAllText(Path.Combine(standalone, "book.yaml"), "title: Lone\n");
            File.WriteAllText(Path.Combine(standalone, "Chapters", "1-start.md"), "# Start\n\nHi.\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            var book = snapshot.Catalog.Fiction[0].Books.Single(b => b.Address.BookId == "lone-novel");
            await Assert.That(book.Address.SeriesId).IsNull();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task HiddenReferencePaths_AreExcluded()
    {
        var root = CreateNmpFixture();
        try
        {
            var archive = Path.Combine(
                root, "src", "Fiction", "galactic-confederation", "References", "_archive");
            Directory.CreateDirectory(archive);
            File.WriteAllText(Path.Combine(archive, "secret.md"), "# Secret\n\nHidden.\n");

            var snapshot = ProtocolWorkspace.Open(root).Read();
            await Assert.That(snapshot.Catalog.Fiction[0].References
                .Any(r => r.Id.Contains("_archive", StringComparison.Ordinal))).IsFalse();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string CreateNmpFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "nmp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        File.WriteAllText(Path.Combine(root, "manuscript.yaml"), """
            protocol: novolis.manuscript
            version: 1
            defaults:
              authors:
                - Frank R. Haugen
              language: en-US
            """);

        var universe = Path.Combine(root, "src", "Fiction", "galactic-confederation");
        Directory.CreateDirectory(Path.Combine(universe, "References", "history"));
        File.WriteAllText(Path.Combine(universe, "universe.yaml"), """
            title: Galactic Confederation
            defaults:
              language: en-US
            """);
        File.WriteAllText(Path.Combine(universe, "References", "history", "timeline.md"), """
            # Timeline

            Events.
            """);

        var series = Path.Combine(universe, "the-calypso-cycle");
        Directory.CreateDirectory(Path.Combine(series, "References", "continuity"));
        File.WriteAllText(Path.Combine(series, "series.yaml"), """
            title: The Calypso Cycle
            """);
        File.WriteAllText(Path.Combine(series, "References", "continuity", "series-timeline.md"), """
            # Series Timeline

            Series events.
            """);

        var book = Path.Combine(series, "calypso");
        Directory.CreateDirectory(Path.Combine(book, "Chapters"));
        Directory.CreateDirectory(Path.Combine(book, "Appendices"));
        File.WriteAllText(Path.Combine(book, "book.yaml"), """
            title: Calypso
            subtitle: Book One
            order: 1
            """);
        File.WriteAllText(Path.Combine(book, "Chapters", "10-the-rescue.md"), """
            ---
            date: "2496.349"
            system: Centralis Omnis System
            locations:
              - The Hub
              - Calypso
            pov: Marsh
            characters:
              - Marsh
              - Ryn
            status: draft
            tags:
              - maintenance-grid
            ---

            # The Rescue

            The message was brief.
            """);
        File.WriteAllText(Path.Combine(book, "Chapters", "20-first-contact.md"), """
            # First Contact

            Hello.
            """);
        File.WriteAllText(Path.Combine(book, "Chapters", "30-oh-hell-no.md"), """
            # Oh Hell No

            Nope.
            """);

        var subject = Path.Combine(root, "src", "NonFiction", "software-engineering");
        Directory.CreateDirectory(Path.Combine(subject, "programming-fundamentals", "Chapters"));
        File.WriteAllText(Path.Combine(subject, "subject.yaml"), """
            title: Software Engineering
            """);
        File.WriteAllText(Path.Combine(subject, "programming-fundamentals", "book.yaml"), """
            title: Programming Fundamentals with C# and .NET
            """);
        File.WriteAllText(
            Path.Combine(subject, "programming-fundamentals", "Chapters", "1-intro.md"),
            "# Intro\n\nWelcome.\n");

        return root;
    }
}
