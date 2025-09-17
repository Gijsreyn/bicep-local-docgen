using Markdig;
using Markdig.Syntax;

namespace Bicep.LocalDeploy.DocGenerator.Services.Formatting
{
    /// <summary>
    /// Validates and processes markdown content using the Markdig library.
    /// </summary>
    public static class MarkdownValidator
    {
        /// <summary>
        /// Validates that the provided markdown content can be parsed correctly by Markdig.
        /// </summary>
        /// <param name="markdownContent">The markdown content to validate.</param>
        /// <returns>The original content if valid, or throws an exception if invalid.</returns>
        public static string ValidateAndProcess(string markdownContent)
        {
            // Create a pipeline with YAML front matter and advanced extensions
            MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseAdvancedExtensions()
                .Build();

            // Parse the markdown to validate it - this will throw if invalid
            _ = Markdown.Parse(markdownContent, pipeline);

            // Return the original content since it's already well-formatted
            // We use Markdig for validation but trust our StringBuilder formatting
            // to preserve YAML front matter and maintain consistent output
            return markdownContent;
        }

        /// <summary>
        /// Creates a configured Markdig pipeline with standard extensions.
        /// </summary>
        /// <returns>A configured MarkdownPipeline instance.</returns>
        public static MarkdownPipeline CreatePipeline()
        {
            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseAdvancedExtensions()
                .Build();
        }

        /// <summary>
        /// Parses markdown content using the standard pipeline configuration.
        /// </summary>
        /// <param name="markdownContent">The markdown content to parse.</param>
        /// <returns>A parsed MarkdownDocument.</returns>
        public static MarkdownDocument Parse(string markdownContent)
        {
            MarkdownPipeline pipeline = CreatePipeline();
            return Markdown.Parse(markdownContent, pipeline);
        }
    }
}
