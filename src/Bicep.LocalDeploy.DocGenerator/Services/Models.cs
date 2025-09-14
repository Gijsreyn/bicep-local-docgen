namespace Bicep.LocalDeploy.DocGenerator.Services
{
    /// <summary>
    /// Analysis results from Roslyn for discovered types and their metadata.
    /// </summary>
    public sealed class AnalysisResult
    {
        /// <summary>Discovered types.</summary>
        public List<TypeInfoModel> Types { get; } = [];
    }

    /// <summary>
    /// Describes a resource or related type including members and documentation metadata.
    /// </summary>
    public sealed class TypeInfoModel
    {
        /// <summary>Type name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Namespace name.</summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>Resource type name from [ResourceType], when present.</summary>
        public string? ResourceTypeName { get; set; }

        /// <summary>Source file path.</summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>Type members (properties).</summary>
        public List<MemberInfoModel> Members { get; } = [];

        /// <summary>Base type names (unqualified).</summary>
        public List<string> BaseTypes { get; } = [];

        /// <summary>YAML front matter blocks (in order).</summary>
        public List<Dictionary<string, string>> FrontMatterBlocks { get; } = [];

        /// <summary>Heading H1 title (separate from front matter).</summary>
        public string? HeadingTitle { get; set; }

        /// <summary>Heading description (separate from front matter).</summary>
        public string? HeadingDescription { get; set; }

        /// <summary>Examples to render.</summary>
        public List<ExampleModel> Examples { get; } = [];

        /// <summary>Custom sections to render.</summary>
        public List<CustomSectionModel> CustomSections { get; } = [];
    }

    /// <summary>
    /// Describes a member (property) and its doc metadata.
    /// </summary>
    public sealed class MemberInfoModel
    {
        /// <summary>Property name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Property type (string representation).</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Documentation description.</summary>
        public string? Description { get; set; }

        /// <summary>True if required.</summary>
        public bool IsRequired { get; set; }

        /// <summary>True if read-only (output attribute).</summary>
        public bool IsReadOnly { get; set; }

        /// <summary>True if identifier.</summary>
        public bool IsIdentifier { get; set; }

        /// <summary>True if enum type.</summary>
        public bool IsEnum { get; set; }

        /// <summary>Enum values, when applicable.</summary>
        public List<string> EnumValues { get; set; } = [];
    }

    /// <summary>
    /// Example snippet metadata.
    /// </summary>
    public sealed class ExampleModel
    {
        /// <summary>Example title.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Example description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Example code content.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Code language (defaults to bicep).</summary>
        public string Language { get; set; } = "bicep";
    }

    /// <summary>
    /// Custom documentation section metadata.
    /// </summary>
    public sealed class CustomSectionModel
    {
        /// <summary>Section title.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Section description.</summary>
        public string Description { get; set; } = string.Empty;
    }
}
