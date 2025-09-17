using System.Globalization;
using System.Text;

namespace Bicep.LocalDeploy.DocGenerator.Services.Generation
{
    /// <summary>
    /// Generates example usage sections for markdown documents.
    /// </summary>
    public static class ExampleGenerator
    {
        /// <summary>
        /// Generates custom examples section from provided example models.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="examples">The custom examples to generate.</param>
        public static void GenerateCustomExamples(StringBuilder sb, List<ExampleModel> examples)
        {
            sb.AppendLine("## Example usage");
            sb.AppendLine();

            foreach (ExampleModel ex in examples)
            {
                if (!string.IsNullOrWhiteSpace(ex.Title))
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"### {ex.Title}");
                    sb.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(ex.Description))
                {
                    sb.AppendLine(ex.Description);
                    sb.AppendLine();
                }
                string lang = string.IsNullOrWhiteSpace(ex.Language) ? "bicep" : ex.Language;
                sb.AppendLine(CultureInfo.InvariantCulture, $"```{lang}");
                string code = (ex.Code ?? string.Empty).Trim('\r', '\n');
                sb.Append(code);
                if (code.Length > 0 && code[^1] is not '\n' and not '\r')
                {
                    sb.AppendLine();
                }
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Generates default examples section based on resource arguments.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="resourceName">The name of the resource.</param>
        /// <param name="requiredArgs">Required arguments for the resource.</param>
        /// <param name="optionalArgs">Optional arguments for the resource.</param>
        public static void GenerateDefaultExamples(
            StringBuilder sb,
            string resourceName,
            List<MemberInfoModel> requiredArgs,
            List<MemberInfoModel> optionalArgs
        )
        {
            sb.AppendLine("## Example usage");
            sb.AppendLine();

            // Basic example (only required)
            sb.AppendLine(CultureInfo.InvariantCulture, $"### Basic {resourceName}");
            sb.AppendLine();
            sb.AppendLine(
                CultureInfo.InvariantCulture,
                $"Creating a basic {resourceName} resource:"
            );
            sb.AppendLine();
            sb.AppendLine("```bicep");
            sb.Append(BicepCodeGenerator.Generate(resourceName, requiredArgs));
            sb.AppendLine("```");
            sb.AppendLine();

            // If there are optional args, add an advanced example
            if (optionalArgs.Count > 0)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"### Advanced {resourceName}");
                sb.AppendLine();
                sb.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"Creating a {resourceName} resource with optional settings:"
                );
                sb.AppendLine();
                sb.AppendLine("```bicep");
                List<MemberInfoModel> all = [.. requiredArgs, .. optionalArgs];
                sb.Append(BicepCodeGenerator.Generate(resourceName, all));
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }
    }
}
