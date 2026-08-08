namespace Novolis.Manuscript.Unit;

public sealed class ManuscriptPathsTests
{
    [Test]
    public async Task ResolveChaptersDirectory_finds_Chapters()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-manuscript-paths-" + Guid.NewGuid().ToString("N"));
        var chapters = Path.Combine(root, "Chapters");
        Directory.CreateDirectory(chapters);
        try
        {
            var resolved = ManuscriptPaths.ResolveChaptersDirectory(root);
            await Assert.That(resolved).IsEqualTo(Path.GetFullPath(chapters));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }

    [Test]
    public async Task ResolveChaptersDirectory_finds_lowercase_chapters()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-manuscript-paths-" + Guid.NewGuid().ToString("N"));
        var chapters = Path.Combine(root, "chapters");
        Directory.CreateDirectory(chapters);
        try
        {
            var resolved = ManuscriptPaths.ResolveChaptersDirectory(root);
            await Assert.That(string.Equals(resolved, Path.GetFullPath(chapters), StringComparison.OrdinalIgnoreCase)).IsTrue();
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }

    [Test]
    public async Task ResolveChaptersDirectory_missing_throws()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-manuscript-paths-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            await Assert.That(() => ManuscriptPaths.ResolveChaptersDirectory(root))
                .ThrowsExactly<DirectoryNotFoundException>();
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }
}
