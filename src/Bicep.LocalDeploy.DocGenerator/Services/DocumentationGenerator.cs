using System.Globalization;
using System.Text;
using Bicep.LocalDeploy.DocGenerator.Services.Formatting;
using Bicep.LocalDeploy.DocGenerator.Services.Generation;

namespace Bicep.LocalDeploy.DocGenerator.Services
{
    /// <summary>
    /// Generates Markdown documentation files from analyzed Bicep model metadata.
    /// </summary>
    public sealed class DocumentationGenerator
    {
        /// <summary>
        /// Generates documentation for all resource types found by the analyzer and writes files to the output directory.
        /// </summary>
        /// <param name="options">Generation options controlling source, output, verbosity, and overwrite behavior.</param>
        public static async Task GenerateAsync(GenerationOptions options)
        {
            if (!options.OutputDirectory.Exists)
            {
                options.OutputDirectory.Create();
            }

            AnalysisResult analysis = await RoslynAnalyzer.AnalyzeAsync(options);

            // Only consider types with ResourceType attribute
            List<TypeInfoModel> resources =
            [
                .. analysis.Types.Where(t => t.ResourceTypeName is not null),
            ];

            if (options.Verbose)
            {
                Console.WriteLine(
                    $"Generating docs for {resources.Count} resource type(s) to '{options.OutputDirectory.FullName}'..."
                );
            }

            // Build lookup for type nesting
            IReadOnlyDictionary<string, TypeInfoModel> typeLookup = analysis.Types.ToDictionary(
                t => t.Name,
                t => t
            );

            foreach (TypeInfoModel type in resources)
            {
                string md = GenerateMarkdownForType(type, typeLookup);
                string name = (type.ResourceTypeName ?? type.Name).ToLowerInvariant();
                string file = Path.Combine(options.OutputDirectory.FullName, $"{name}.md");
                bool exists = File.Exists(file);

                if (exists && !options.Force)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(
                        $"Warning: {file} already exists. Skipping. Use --force to overwrite."
                    );
                    Console.ResetColor();
                    continue;
                }

                FileMode mode = exists ? FileMode.Truncate : FileMode.Create;
                using FileStream stream = new(
                    file,
                    mode,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    useAsync: true
                );
                using StreamWriter writer = new(stream, Encoding.UTF8);
                await writer.WriteAsync(md);

                if (options.Verbose && !exists)
                {
                    Console.WriteLine($"File write: {file}");
                }
            }
        }

        private static string GenerateMarkdownForType(
            TypeInfoModel type,
            IReadOnlyDictionary<string, TypeInfoModel> typeLookup
        )
        {
            string resourceName = type.ResourceTypeName ?? type.Name;

            List<MemberInfoModel> requiredArgs =
            [
                .. type.Members.Where(m => m.IsRequired && !m.IsReadOnly).OrderBy(m => m.Name),
            ];

            List<MemberInfoModel> optionalArgs =
            [
                .. type.Members.Where(m => !m.IsRequired && !m.IsReadOnly).OrderBy(m => m.Name),
            ];

            List<MemberInfoModel> outputs =
            [
                .. type.Members.Where(m => m.IsReadOnly).OrderBy(m => m.Name),
            ];

            StringBuilder sb = new();

            // Generate YAML front matter
            FrontMatterGenerator.Generate(sb, type.FrontMatterBlocks);

            // Generate heading and description
            string? fmTitle = FrontMatterGenerator.GetValue(
                type.FrontMatterBlocks.FirstOrDefault() ?? [],
                "title"
            );
            string title = type.HeadingTitle ?? fmTitle ?? type.ResourceTypeName ?? "<handlerName>";
            sb.AppendLine(CultureInfo.InvariantCulture, $"# {title}");
            sb.AppendLine();

            // Description under H1
            if (!string.IsNullOrWhiteSpace(type.HeadingDescription))
            {
                sb.AppendLine(type.HeadingDescription);
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"Manages {resourceName} resources.");
            }
            sb.AppendLine();

            // Generate examples
            if (type.Examples.Count > 0)
            {
                ExampleGenerator.GenerateCustomExamples(sb, type.Examples);
            }
            else
            {
                ExampleGenerator.GenerateDefaultExamples(
                    sb,
                    resourceName,
                    requiredArgs,
                    optionalArgs
                );
            }

            // Generate argument reference
            if (requiredArgs.Count + optionalArgs.Count > 0)
            {
                ReferenceGenerator.GenerateArgumentReference(
                    sb,
                    requiredArgs,
                    optionalArgs,
                    typeLookup
                );
            }

            // Generate attribute reference
            if (outputs.Count > 0)
            {
                ReferenceGenerator.GenerateAttributeReference(sb, outputs, typeLookup);
            }

            // Generate custom sections
            if (type.CustomSections.Count > 0)
            {
                ReferenceGenerator.GenerateCustomSections(sb, type.CustomSections);
            }

            // Validate and return the markdown
            string rawMarkdown = sb.ToString();
            return MarkdownValidator.ValidateAndProcess(rawMarkdown);
        }
    }
}
