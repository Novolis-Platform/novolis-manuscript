using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Metrics;

/// <summary>Per-chapter metrics row.</summary>
public sealed class ChapterMetricRow
{
    /// <summary>Chapter file name.</summary>
    public string File { get; init; } = "";

    /// <summary>Approximate word count.</summary>
    public int Words { get; init; }

    /// <summary>TODO/FIXME/TK needle count.</summary>
    public int Todos { get; init; }
}
