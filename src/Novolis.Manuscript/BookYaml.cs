using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;

namespace Novolis.Manuscript;

/// <summary>Thin YAML helpers for <c>series.yaml</c> / <c>book.yaml</c>.</summary>
[ExcludeFromCodeCoverage(Justification = "Thin YAML map helpers; print remodel covers via exporters.")]
public static class BookYaml
{
    static readonly IDeserializer Deserializer =
        new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

    /// <summary>Loads a YAML object map from a file (empty if missing).</summary>
    public static Dictionary<string, object?> LoadFile(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var raw = File.ReadAllText(path);
        var obj = Deserializer.Deserialize<Dictionary<object, object?>>(raw);
        if (obj is null)
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in obj)
            result[key?.ToString() ?? string.Empty] = value;
        return result;
    }

    /// <summary>Gets a trimmed string value.</summary>
    public static string? GetString(Dictionary<string, object?> dict, string key) =>
        dict.TryGetValue(key, out var v) && v is not null ? v.ToString()?.Trim() : null;

    /// <summary>Gets a boolean value with a default.</summary>
    public static bool GetBool(Dictionary<string, object?> dict, string key, bool defaultValue = false)
    {
        if (!dict.TryGetValue(key, out var v) || v is null)
            return defaultValue;
        if (v is bool b)
            return b;
        return bool.TryParse(v.ToString(), out var parsed) && parsed;
    }
}
