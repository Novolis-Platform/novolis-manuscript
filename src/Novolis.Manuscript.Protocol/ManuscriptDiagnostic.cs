namespace Novolis.Manuscript.Protocol;

/// <summary>One protocol diagnostic finding.</summary>
public sealed record ManuscriptDiagnostic(
    ManuscriptDiagnosticSeverity Severity,
    string Code,
    string Message,
    string Path);
