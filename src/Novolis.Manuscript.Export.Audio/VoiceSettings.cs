using System.Diagnostics.CodeAnalysis;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>Voice and planner settings for manuscript TTS and audiobook generation.</summary>
[ExcludeFromCodeCoverage(Justification = "TTS voice defaults orthogonal to print remodel.")]
public sealed class VoiceSettings
{
    /// <summary>Azure Speech voice short name (default: book narrator Ava).</summary>
    public string Voice { get; init; } = "en-US-AvaMultilingualNeural";

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
}
