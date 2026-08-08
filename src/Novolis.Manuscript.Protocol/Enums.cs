namespace Novolis.Manuscript.Protocol;

/// <summary>Fiction vs non-fiction branch.</summary>
public enum ManuscriptKind
{
    /// <summary>Under <c>src/Fiction</c>.</summary>
    Fiction,

    /// <summary>Under <c>src/NonFiction</c>.</summary>
    NonFiction,
}

/// <summary>Chapter or appendix document kind.</summary>
public enum ManuscriptDocumentKind
{
    /// <summary>Primary manuscript document under <c>Chapters</c>.</summary>
    Chapter,

    /// <summary>Publishable appendix under <c>Appendices</c>.</summary>
    Appendix,
}

/// <summary>Diagnostic severity.</summary>
public enum ManuscriptDiagnosticSeverity
{
    /// <summary>Non-fatal authoring issue.</summary>
    Warning,

    /// <summary>Structural or metadata error.</summary>
    Error,
}
