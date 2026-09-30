using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Metrics;

/// <summary>One character's POV and cast chapter lists.</summary>
public sealed class CharacterSlice
{
    /// <summary>Chapters where this name appears in POV.</summary>
    public List<CharacterSliceChapter> Pov { get; } = [];

    /// <summary>Chapters where this name appears in cast/characters.</summary>
    public List<CharacterSliceChapter> Characters { get; } = [];
}
