using Novolis.Manuscript.Protocol;

namespace Novolis.Manuscript;

/// <summary>Severity of a diagnostic finding.</summary>
public enum DiagnosticSeverity
{
    /// <summary>Informational note.</summary>
    Info,

    /// <summary>Non-fatal issue.</summary>
    Warning,

    /// <summary>Blocking structural problem.</summary>
    Error,
}
