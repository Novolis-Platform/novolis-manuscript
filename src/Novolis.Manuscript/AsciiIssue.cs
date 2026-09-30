using System.Text;

namespace Novolis.Manuscript;

/// <summary>One non-ASCII codepoint remaining after known replacements (or during scan).</summary>
public sealed record AsciiIssue(string Path, int Line, int Column, int Codepoint, int Index);
