using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Metrics;

/// <summary>Chapter row used for character slice reports.</summary>
public sealed record CharacterSliceChapter(
    string FileName,
    int? Number,
    string Title,
    string? PovRaw,
    string? CharactersRaw,
    IReadOnlyList<string> PovNames,
    IReadOnlyList<string> CharacterNames);
