using Markdig;

namespace Novolis.Manuscript.Export.Pdf;

/// <summary>Shared Markdig pipeline for book and reference print.</summary>
internal static class MarkdownRenderPipeline
{
    internal static readonly MarkdownPipeline Instance =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
}
