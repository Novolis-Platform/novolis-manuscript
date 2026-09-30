using Novolis.Manuscript.Protocol;

namespace Novolis.Manuscript;

/// <summary>One manuscript doctor finding.</summary>
public sealed record DiagnosticFinding(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Path = null);
