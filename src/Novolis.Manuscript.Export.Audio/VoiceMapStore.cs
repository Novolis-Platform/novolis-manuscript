using System.Diagnostics.CodeAnalysis;
using System.Text;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>
/// Loads and saves voice-map YAML compatible with books <c>tools/audio/voice-map.yaml</c>
/// (nested <c>narrator</c> / <c>pauses</c> / <c>generation</c> / <c>pronunciation</c>).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Voice map I/O orthogonal to print remodel.")]
public static class VoiceMapStore
{
    static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>Reads voice settings from a YAML file.</summary>
    public static VoiceSettings Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var yaml = File.ReadAllText(path);
        return LoadFromYaml(yaml);
    }

    /// <summary>Reads voice settings from YAML text.</summary>
    public static VoiceSettings LoadFromYaml(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        var dto = Deserializer.Deserialize<VoiceMapDto>(yaml) ?? new VoiceMapDto();
        return dto.ToSettings();
    }

    /// <summary>Writes voice settings to a YAML file.</summary>
    public static void Save(string path, VoiceSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(settings);
        var yaml = SaveToYaml(settings);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, yaml);
    }

    /// <summary>Serializes voice settings to books-compatible nested YAML text.</summary>
    public static string SaveToYaml(VoiceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var sb = new StringBuilder();
        sb.AppendLine("# Canonical single-narrator configuration.");
        sb.AppendLine("#");
        sb.AppendLine("# All prose and quoted dialogue use this voice. The C# generator deliberately");
        sb.AppendLine("# does not parse speakers or maintain a character cast.");
        sb.AppendLine("narrator:");
        sb.AppendLine($"  voice: {settings.Voice}");
        sb.AppendLine($"  rate: \"{FormatPercent(settings.RatePercent)}\"");
        sb.AppendLine($"  pitch: \"{FormatHertz(settings.PitchHertz)}\"");
        sb.AppendLine($"  volume: \"{FormatPercent(settings.VolumePercent)}\"");
        sb.AppendLine();
        sb.AppendLine("pauses:");
        sb.AppendLine($"  scene_break_ms: {settings.SceneBreakMs}");
        sb.AppendLine();
        sb.AppendLine("generation:");
        sb.AppendLine($"  max_chunk_chars: {settings.MaxChunkChars}");
        sb.AppendLine();
        sb.AppendLine("# Spoken-text rewrites for Azure Speech (manuscript files stay unchanged).");
        sb.AppendLine("# Match whole words only, longest keys first. Values are phonetic spellings");
        sb.AppendLine("# the neural voice is more likely to say correctly.");
        sb.AppendLine("pronunciation:");
        if (settings.Pronunciation.Count == 0)
        {
            sb.AppendLine("  {}");
        }
        else
        {
            foreach (var (key, value) in settings.Pronunciation.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"  {EscapeYamlKey(key)}: \"{EscapeYamlDoubleQuoted(value)}\"");
        }

        return sb.ToString();
    }

    static string EscapeYamlKey(string key) =>
        key.Contains(':') || key.Contains(' ') || key.Contains('#')
            ? $"\"{EscapeYamlDoubleQuoted(key)}\""
            : key;

    static string EscapeYamlDoubleQuoted(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

    sealed class VoiceMapDto
    {
        public NarratorDto? Narrator { get; init; }
        public PausesDto? Pauses { get; init; }
        public GenerationDto? Generation { get; init; }
        public Dictionary<string, string>? Pronunciation { get; init; }

        public VoiceSettings ToSettings()
        {
            var narrator = Narrator ?? new NarratorDto();
            var voice = string.IsNullOrWhiteSpace(narrator.Voice)
                ? "en-US-AvaMultilingualNeural"
                : narrator.Voice.Trim();
            var rate = ParsePercent(narrator.Rate, -4);
            var pitch = ParseHertz(narrator.Pitch, 0);
            var volume = ParsePercent(narrator.Volume, 0);

            return new VoiceSettings
            {
                Voice = voice,
                RatePercent = rate,
                PitchHertz = pitch,
                VolumePercent = volume,
                SceneBreakMs = Pauses?.SceneBreakMs ?? 1200,
                PauseMs = 500,
                MaxChunkChars = Generation?.MaxChunkChars ?? 2800,
                Pronunciation = Pronunciation ??
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            };
        }

        static int ParsePercent(string? text, int fallback)
        {
            if (string.IsNullOrWhiteSpace(text))
                return fallback;
            var value = text.Trim().TrimEnd('%');
            return int.TryParse(
                value,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed)
                ? parsed
                : fallback;
        }

        static int ParseHertz(string? text, int fallback)
        {
            if (string.IsNullOrWhiteSpace(text))
                return fallback;
            var value = text.Trim();
            if (value.EndsWith("Hz", StringComparison.OrdinalIgnoreCase))
                value = value[..^2];
            return int.TryParse(
                value,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed)
                ? parsed
                : fallback;
        }
    }

    sealed class NarratorDto
    {
        public string? Voice { get; init; }
        public string? Rate { get; init; }
        public string? Pitch { get; init; }
        public string? Volume { get; init; }
    }

    sealed class PausesDto
    {
        public int? SceneBreakMs { get; init; }
    }

    sealed class GenerationDto
    {
        public int? MaxChunkChars { get; init; }
    }

    static string FormatPercent(int value) => value switch
    {
        > 0 => $"+{value}%",
        _ => $"{value}%",
    };

    static string FormatHertz(int value) => value switch
    {
        > 0 => $"+{value}Hz",
        _ => $"{value}Hz",
    };
}
