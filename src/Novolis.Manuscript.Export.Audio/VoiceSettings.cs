using System.Diagnostics.CodeAnalysis;
using Novolis.Audio.Voice.EdgeTts;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>Voice and planner settings for manuscript TTS and audiobook generation.</summary>
[ExcludeFromCodeCoverage(Justification = "TTS voice defaults orthogonal to print remodel.")]
public sealed class VoiceSettings
{
    /// <summary>Neural voice short name (Azure or Edge; default: book narrator Ava).</summary>
    public string Voice { get; init; } = "en-US-AvaNeural";

    /// <summary>Prosody rate in percent (default: −4% to match book narrator).</summary>
    public int RatePercent { get; init; } = -4;

    /// <summary>Prosody pitch in hertz.</summary>
    public int PitchHertz { get; init; }

    /// <summary>Prosody volume in percent.</summary>
    public int VolumePercent { get; init; }

    /// <summary>Pause inserted between scene breaks when planning chapters (ms).</summary>
    public int SceneBreakMs { get; init; } = 1200;

    /// <summary>Default pause duration for generic pause segments (ms).</summary>
    public int PauseMs { get; init; } = 500;

    /// <summary>Maximum characters per spoken chunk.</summary>
    public int MaxChunkChars { get; init; } = 2800;

    /// <summary>Whole-word pronunciation rewrites (longest keys first).</summary>
    public IReadOnlyDictionary<string, string> Pronunciation { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps to <see cref="SpeechOptions"/> for <see cref="SpeechPlanner"/>.</summary>
    public SpeechOptions ToSpeechOptions() => new()
    {
        SceneBreakMs = SceneBreakMs,
        MaxChunkChars = MaxChunkChars,
        Pronunciation = Pronunciation,
    };

    /// <summary>Maps to Edge Read Aloud synthesis options (curated voice fallback: Ava).</summary>
    public EdgeTtsSynthesisOptions ToEdgeTtsOptions() => new()
    {
        Voice = ResolveEdgeVoice(Voice),
        Rate = new ProsodyPercent(RatePercent),
        Pitch = new ProsodyHertz(PitchHertz),
        Volume = new ProsodyPercent(VolumePercent),
    };

    static EdgeVoice ResolveEdgeVoice(string? shortName)
    {
        if (EdgeVoiceCatalog.TryParse(shortName, out var voice))
            return voice;

        // Azure Multilingual short names map onto the closest curated Edge voice.
        if (!string.IsNullOrWhiteSpace(shortName)
            && shortName.Contains("Multilingual", StringComparison.OrdinalIgnoreCase))
        {
            var trimmed = shortName.Replace("Multilingual", string.Empty, StringComparison.OrdinalIgnoreCase);
            if (EdgeVoiceCatalog.TryParse(trimmed, out voice))
                return voice;
        }

        return EdgeVoice.EnUsAva;
    }
}
