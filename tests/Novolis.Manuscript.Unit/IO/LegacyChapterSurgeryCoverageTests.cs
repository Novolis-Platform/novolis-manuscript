using Novolis.Manuscript.IO;

namespace Novolis.Manuscript.Unit;

public sealed class LegacyChapterSurgeryCoverageTests
{
    [Test]
    public async Task InsertBetween_apply_writes_booktools_stub()
    {
        var chapters = CreateChapters();
        try
        {
            var result = LegacyChapterSurgery.InsertBetween(chapters, 1.5, "Interlude", apply: true);
            await Assert.That(result.Applied).IsTrue();
            var files = Directory.GetFiles(chapters, "*.md");
            await Assert.That(files.Length).IsEqualTo(3);
            var interlude = files.Single(f => Path.GetFileName(f).Contains("interlude", StringComparison.OrdinalIgnoreCase));
            var text = await File.ReadAllTextAsync(interlude);
            await Assert.That(text).Contains("booktools-chapter: 1.5");
            await Assert.That(text).Contains("# Chapter 1.5 - Interlude");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(chapters)!, recursive: true);
        }
    }

    [Test]
    public async Task InsertAfter_apply_bumps_and_syncs()
    {
        var chapters = CreateChapters();
        try
        {
            var result = LegacyChapterSurgery.InsertAfter(chapters, 1, "Inserted", apply: true);
            await Assert.That(result.Applied).IsTrue();
            var names = Directory.GetFiles(chapters, "*.md").Select(Path.GetFileName).OrderBy(x => x).ToArray();
            await Assert.That(names.Length).IsEqualTo(3);
            await Assert.That(names.Any(n => n!.Contains("inserted", StringComparison.OrdinalIgnoreCase))).IsTrue();
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(chapters)!, recursive: true);
        }
    }

    [Test]
    public async Task PromoteDecimal_and_sync_filenames()
    {
        var root = Directory.CreateTempSubdirectory("ms-surg-").FullName;
        var chapters = Path.Combine(root, "chapters");
        Directory.CreateDirectory(chapters);
        try
        {
            File.WriteAllText(Path.Combine(chapters, "01-000-alpha.md"), """
                <!-- booktools-chapter: 1.25 -->

                # Chapter 1.25 - Alpha

                Body.
                """);
            File.WriteAllText(Path.Combine(chapters, "02-beta.md"), """
                ---
                chapter: 2
                ---

                # Chapter 2 - Beta

                Body.
                """);

            var promote = LegacyChapterSurgery.PromoteDecimal(chapters, 1.25, 2, apply: true);
            await Assert.That(promote.Applied).IsTrue();

            var syncDry = LegacyChapterSurgery.SyncFilenames(chapters, apply: false);
            await Assert.That(syncDry.Applied).IsFalse();
            var sync = LegacyChapterSurgery.SyncFilenames(chapters, apply: true);
            await Assert.That(sync.Applied).IsTrue();

            var dryInsert = LegacyChapterSurgery.InsertAfter(chapters, 2, "Later", apply: false);
            await Assert.That(dryInsert.Applied).IsFalse();
            await Assert.That(dryInsert.Message).Contains("dry run");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Metadata_sources_yaml_frontmatter_and_frontmatter_filename()
    {
        var root = Directory.CreateTempSubdirectory("ms-meta-").FullName;
        var chapters = Path.Combine(root, "chapters");
        Directory.CreateDirectory(chapters);
        try
        {
            File.WriteAllText(Path.Combine(chapters, "00-frontmatter.md"), "# Front\n\n");
            File.WriteAllText(Path.Combine(chapters, "empty.md"), "");
            File.WriteAllText(Path.Combine(chapters, "01-yaml.md"), """
                ---
                chapter: 1
                ---

                # Chapter 1 - Yaml Title

                Body.
                """);
            File.WriteAllText(Path.Combine(chapters, "none.md"), "no chapter marker\n");

            var sync = LegacyChapterSurgery.SyncFilenames(chapters, apply: true);
            await Assert.That(sync.Applied).IsTrue();
            await Assert.That(() => LegacyChapterSurgery.InsertBetween(chapters, 1, "Dup", apply: false))
                .ThrowsExactly<InvalidOperationException>();
            await Assert.That(() => LegacyChapterSurgery.PromoteDecimal(chapters, 9, 10, apply: false))
                .ThrowsExactly<InvalidOperationException>();
            await Assert.That(() => LegacyChapterSurgery.PromoteDecimal(chapters, 1, 1.5, apply: false))
                .ThrowsExactly<InvalidOperationException>();
            await Assert.That(() => LegacyChapterSurgery.InsertAfter(chapters, 99, "Nope", apply: false))
                .ThrowsExactly<InvalidOperationException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task WorkingCopy_creates_recovery_store()
    {
        var root = Directory.CreateTempSubdirectory("ms-wc-").FullName;
        try
        {
            var store = ManuscriptWorkingCopy.CreateRecoveryStore(root);
            await Assert.That(store.RootDirectory).IsEqualTo(Path.Combine(root, ".writer", "recovery"));
            store.WriteSnapshot("doc", "hello");
            await Assert.That(Directory.Exists(store.RootDirectory)).IsTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Surgery_collision_bom_and_error_paths()
    {
        var root = Directory.CreateTempSubdirectory("ms-surg2-").FullName;
        try
        {
            var empty = Path.Combine(root, "empty");
            Directory.CreateDirectory(empty);
            var syncEmpty = LegacyChapterSurgery.SyncFilenames(empty, apply: false);
            await Assert.That(syncEmpty.Applied).IsFalse();

            File.WriteAllText(Path.Combine(empty, "1.md"), "\uFEFF<!-- booktools-chapter: 1 -->\n\n# Chapter 1 - !!!\n\n");
            File.WriteAllText(Path.Combine(empty, "1-chapter.md"), "<!-- booktools-chapter: 1.5 -->\n\n# Chapter 1.5 - !!!\n\n");
            var sync = LegacyChapterSurgery.SyncFilenames(empty, apply: true);
            await Assert.That(sync.Applied).IsTrue();

            var dup = Path.Combine(root, "dup");
            Directory.CreateDirectory(dup);
            File.WriteAllText(Path.Combine(dup, "a.md"), "# Chapter 1.5 - A\n\n");
            File.WriteAllText(Path.Combine(dup, "b.md"), "# Chapter 1.5 - B\n\n");
            await Assert.That(() => LegacyChapterSurgery.PromoteDecimal(dup, 1.5, 2, apply: false))
                .ThrowsExactly<InvalidOperationException>();

            var between = Path.Combine(root, "between");
            Directory.CreateDirectory(between);
            File.WriteAllText(Path.Combine(between, "01-one.md"), "# Chapter 1 - One\n\n");
            await Assert.That(() => LegacyChapterSurgery.InsertBetween(between, 1.5, "Dupe", apply: true))
                .IsNotNull();
            // Second insert with same key fails
            await Assert.That(() => LegacyChapterSurgery.InsertBetween(between, 1.5, "Dupe", apply: true))
                .ThrowsExactly<InvalidOperationException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string CreateChapters()
    {
        var root = Directory.CreateTempSubdirectory("ms-surg-").FullName;
        var chapters = Path.Combine(root, "chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(chapters, "01-one.md"), "# Chapter 1 - One\n\nBody.\n");
        File.WriteAllText(Path.Combine(chapters, "02-two.md"), "# Chapter 2 - Two\n\nBody.\n");
        return chapters;
    }
}
