namespace Novolis.Manuscript.Editorial;

/// <summary>Stable finding codes for editorial diagnostics.</summary>
public static class EditorialCodes
{
    /// <summary>Forbidden lexicon token (wrong-universe / wrong-tech).</summary>
    public const string LexiconForbid = "editorial-lexicon-forbid";

    /// <summary>Prefer an alternate term (ship/station habit).</summary>
    public const string LexiconPrefer = "editorial-lexicon-prefer";

    /// <summary>Correlative negation pattern (<c>Not X. Y.</c>).</summary>
    public const string SlopCorrelativeNegation = "editorial-slop-correlative-negation";

    /// <summary>Answer-as-question fragment.</summary>
    public const string SlopAnswerAsQuestion = "editorial-slop-answer-as-question";

    /// <summary>Unearned profundity beat without concrete anchor.</summary>
    public const string SlopUnearnedProfundity = "editorial-slop-unearned-profundity";

    /// <summary>Known naming / spelling variant of a canonical entity.</summary>
    public const string NamingVariant = "editorial-naming-variant";
}
