using System.Text;

namespace Novolis.Manuscript;

/// <summary>Result of normalizing one file or string to ASCII house style.</summary>
public sealed record AsciiNormalizeResult(
    string Text,
    int Replacements,
    bool HasRemainingNonAscii,
    IReadOnlyList<AsciiIssue> RemainingIssues);
