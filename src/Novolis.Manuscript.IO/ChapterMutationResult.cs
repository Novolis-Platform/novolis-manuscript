using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Novolis.Manuscript.IO;

/// <summary>Result of a chapter-tree mutation (apply or dry-run).</summary>
public sealed class ChapterMutationResult
{
    /// <summary>Creates a mutation result.</summary>
    public ChapterMutationResult(bool applied, string message, object? plan = null)
    {
        Applied = applied;
        Message = message;
        Plan = plan;
    }

    /// <summary>Whether changes were written to disk.</summary>
    public bool Applied { get; }

    /// <summary>Human-readable status.</summary>
    public string Message { get; }

    /// <summary>Optional plan payload for dry-run inspection.</summary>
    public object? Plan { get; }
}
