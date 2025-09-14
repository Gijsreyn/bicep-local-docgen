namespace Bicep.LocalDeploy.DocGenerator.Services
{
    /// <summary>
    /// Options controlling documentation generation (input sources, output location, and behavior).
    /// </summary>
    public sealed class GenerationOptions
    {
        /// <summary>The source directories containing C# model files to analyze.</summary>
    public DirectoryInfo[] SourceDirectories { get; init; } = [];

        /// <summary>File name patterns to include (e.g., "*.cs").</summary>
        public string[] FilePatterns { get; init; } = ["*.cs"];

        /// <summary>The output directory where Markdown files will be written.</summary>
        public DirectoryInfo OutputDirectory { get; init; } = new("./docs");

        /// <summary>When true, prints verbose diagnostic information.</summary>
        public bool Verbose { get; init; }

        /// <summary>When true, overwrites existing files in the output directory.</summary>
        public bool Force { get; init; }
    }
}
