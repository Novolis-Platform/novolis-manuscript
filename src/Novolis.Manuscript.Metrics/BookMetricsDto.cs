using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Novolis.Manuscript;

namespace Novolis.Manuscript.Metrics;

/// <summary>Aggregated book metrics.</summary>
public sealed class BookMetricsDto
{
    /// <summary>Series id (or <c>books</c> for standalone).</summary>
    public string Series { get; init; } = "";

    /// <summary>Book id.</summary>
    public string Book { get; init; } = "";

    /// <summary>Book title from yaml when present.</summary>
    public string? Title { get; init; }

    /// <summary>Target words from book.yaml when present.</summary>
    public int? TargetWords { get; init; }

    /// <summary>Total words across chapters.</summary>
    public int TotalWords { get; init; }

    /// <summary>Total TODO/FIXME/TK counts.</summary>
    public int TotalTodos { get; init; }

    /// <summary>Estimated reading hours at ~9300 words/hour.</summary>
    public double EstimatedHours { get; init; }

    /// <summary>Per-chapter breakdown.</summary>
    public IReadOnlyList<ChapterMetricRow> Chapters { get; init; } = [];
}
