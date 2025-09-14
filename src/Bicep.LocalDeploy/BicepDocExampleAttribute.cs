namespace Bicep.LocalDeploy
{
    /// <summary>
    /// Adds an example block to the generated documentation, including a title, description, code snippet, and optional language (defaults to bicep).
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        AllowMultiple = true,
        Inherited = true
    )]
    public sealed class BicepDocExampleAttribute(
        string title,
        string description,
        string code,
        string? language = null
    ) : Attribute
    {
        /// <summary>The example title (rendered as an H3).</summary>
        public string Title { get; } = title;

        /// <summary>A description paragraph for the example.</summary>
        public string Description { get; } = description;

        /// <summary>The example code snippet.</summary>
        public string Code { get; } = code;

        /// <summary>The code language for fenced code blocks (defaults to "bicep").</summary>
        public string Language { get; } = string.IsNullOrWhiteSpace(language) ? "bicep" : language;
    }
}
