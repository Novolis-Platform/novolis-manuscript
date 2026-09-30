using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Novolis.Manuscript;
using Novolis.Markup.Markdown;

namespace Novolis.Manuscript.Export.Audio;

/// <summary>Planned speech for a chapter.</summary>
public sealed class SpeechPlan
{
    /// <summary>Creates a plan.</summary>
    public SpeechPlan(IReadOnlyList<SpeechSegment> segments, string planHash)
    {
        Segments = segments;
        PlanHash = planHash;
    }

    /// <summary>Ordered segments.</summary>
    public IReadOnlyList<SpeechSegment> Segments { get; }

    /// <summary>Content-addressed hash of the plan.</summary>
    public string PlanHash { get; }
}
