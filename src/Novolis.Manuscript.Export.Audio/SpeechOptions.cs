using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Novolis.Manuscript;
using Novolis.Markup.Markdown;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>Voice / planner settings for manuscript speech.</summary>
public sealed class SpeechOptions
{
    /// <summary>Pause inserted between scene breaks (ms).</summary>
    public int SceneBreakMs { get; init; } = 1200;

    /// <summary>Maximum characters per spoken chunk.</summary>
    public int MaxChunkChars { get; init; } = 2800;

    /// <summary>Whole-word pronunciation rewrites (longest keys first).</summary>
    public IReadOnlyDictionary<string, string> Pronunciation { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
