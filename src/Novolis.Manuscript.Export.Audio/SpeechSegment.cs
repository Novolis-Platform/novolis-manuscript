using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Novolis.Manuscript;
using Novolis.Markup.Markdown;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>One segment in a speech plan.</summary>
public sealed record SpeechSegment(SpeechSegmentKind Kind, string? Text, int PauseMs)
{
    /// <summary>Creates a spoken segment.</summary>
    public static SpeechSegment Spoken(string text) => new(SpeechSegmentKind.Text, text, 0);

    /// <summary>Creates a pause segment.</summary>
    public static SpeechSegment Pause(int milliseconds) => new(SpeechSegmentKind.Pause, null, milliseconds);
}
