using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Novolis.Manuscript;
using Novolis.Markup.Markdown;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>Kind of speech segment.</summary>
public enum SpeechSegmentKind
{
    /// <summary>Spoken text.</summary>
    Text,
    /// <summary>Silence / pause.</summary>
    Pause
}
