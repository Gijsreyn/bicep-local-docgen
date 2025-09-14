using System.Globalization;
using System.Text;

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
            // YAML front matter blocks
            if (type.FrontMatterBlocks.Count > 0)
            {
                foreach (Dictionary<string, string> block in type.FrontMatterBlocks)
                {
                    sb.AppendLine("---");
                    foreach (
                        KeyValuePair<string, string> kvp in block.OrderBy(
                            k => k.Key,
                            StringComparer.Ordinal
                        )
                    )
                    {
                        sb.AppendLine(
                            CultureInfo.InvariantCulture,
                            $"{ToKebab(kvp.Key)}: \"{kvp.Value}\""
                        );
                    }
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }
            // Heading (H1) comes from BicepDocHeading attribute; fall back to front matter title; then resource name
            string? fmTitle = GetFrontMatterValue(
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

            // Examples: if custom examples provided via attributes, render those; else use defaults
            if (type.Examples.Count > 0)
            {
                sb.AppendLine("## Example usage");
                sb.AppendLine();
                foreach (ExampleModel ex in type.Examples)
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
            else
            {
                AppendExamples(sb, resourceName, requiredArgs, optionalArgs);
            }

            // Argument reference
            if (requiredArgs.Count + optionalArgs.Count > 0)
            {
                sb.AppendLine("## Argument reference");
                sb.AppendLine();
                sb.AppendLine("The following arguments are available:");
                sb.AppendLine();

                foreach (MemberInfoModel m in requiredArgs)
                {
                    AppendMemberWithNesting(sb, m, isOutput: false, typeLookup: typeLookup);
                }
                foreach (MemberInfoModel m in optionalArgs)
                {
                    AppendMemberWithNesting(sb, m, isOutput: false, typeLookup: typeLookup);
                }

                sb.AppendLine();
            }

            // Attribute reference
            if (outputs.Count > 0)
            {
                sb.AppendLine("## Attribute reference");
                sb.AppendLine();
                sb.AppendLine(
                    "In addition to all arguments above, the following attributes are outputted:"
                );
                sb.AppendLine();

                foreach (MemberInfoModel m in outputs)
                {
                    AppendMemberWithNesting(sb, m, isOutput: true, typeLookup: typeLookup);
                }

                sb.AppendLine();
            }

            // Custom sections (if any) appended at the end in chronological order
            if (type.CustomSections.Count > 0)
            {
                foreach (CustomSectionModel section in type.CustomSections)
                {
                    if (!string.IsNullOrWhiteSpace(section.Title))
                    {
                        sb.AppendLine(CultureInfo.InvariantCulture, $"## {section.Title}");
                        sb.AppendLine();
                    }
                    if (!string.IsNullOrWhiteSpace(section.Description))
                    {
                        sb.AppendLine(section.Description);
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }

        private static string? GetFrontMatterValue(
            IReadOnlyDictionary<string, string> frontMatter,
            string key
        )
        {
            // Case-insensitive lookup so either "title" or "Title" works
            foreach (KeyValuePair<string, string> kvp in frontMatter)
            {
                if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        private static void AppendExamples(
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
            sb.Append(GenerateBicep(resourceName, requiredArgs));
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
                sb.Append(GenerateBicep(resourceName, all));
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        private static string GenerateBicep(string resourceName, List<MemberInfoModel> props)
        {
            string varName = ToCamel(resourceName);
            StringBuilder sb = new();
            sb.AppendLine(
                CultureInfo.InvariantCulture,
                $"resource {varName} '{resourceName}' = {{"
            );
            foreach (MemberInfoModel p in props)
            {
                sb.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"  {ToCamel(p.Name)}: {ExampleFor(p)}"
                );
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string ExampleFor(MemberInfoModel p)
        {
            string type = p.Type.TrimEnd('?');
            return type switch
            {
                "string" or "String" => p.Name.Contains("path", StringComparison.OrdinalIgnoreCase)
                    ? "'/Path/example/test'"
                    : "'example'",
                "bool" or "Boolean" => "true",
                "int" or "Int32" or "long" or "Int64" => "1",
                "double" or "float" or "Single" or "Decimal" or "decimal" => "1.0",
                _ when type.Contains("[]", StringComparison.Ordinal)
                        || type.Contains("List<", StringComparison.Ordinal) => "[]",
                _ when p.IsEnum && p.EnumValues.Count > 0 => $"'{p.EnumValues.First()}'",
                _ => "{}",
            };
        }

        private static string ToCamel(string name)
        {
            return string.IsNullOrEmpty(name)
                ? name
                : char.ToLowerInvariant(name[0]) + name.AsSpan(1).ToString();
        }

        private static string ToKebab(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }
            StringBuilder sb = new(name.Length + 8);
            foreach (char ch in name)
            {
                if (char.IsWhiteSpace(ch))
                {
                    sb.Append('-');
                }
                else
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
            }
            return sb.ToString();
        }

        private static string FormatEnumSuffix(MemberInfoModel m)
        {
            if (!m.IsEnum || m.EnumValues is null || m.EnumValues.Count == 0)
            {
                return string.Empty;
            }

            // Format values as: `A`, `B`, or `C`
            List<string> values = m.EnumValues;
            if (values.Count == 1)
            {
                return $" (Can be `{values[0]}`)";
            }

            string last = values[^1];
            IEnumerable<string> head = values.Take(values.Count - 1).Select(v => $"`{v}`");
            string joinedHead = string.Join(", ", head);
            return $" (Can be {joinedHead}, or `{last}`)";
        }

        private static void AppendMemberWithNesting(
            StringBuilder sb,
            MemberInfoModel m,
            bool isOutput,
            IReadOnlyDictionary<string, TypeInfoModel> typeLookup
        )
        {
            // Base line
            string baseDesc = isOutput
                ? $"- `{ToCamel(m.Name)}` - {m.Description}{FormatEnumSuffix(m)}"
                : $"- `{ToCamel(m.Name)}` - {(m.IsRequired ? "(Required) " : "(Optional) ")}{m.Description}{FormatEnumSuffix(m)}";

            // Determine nested type by stripping nullable and generics/arrays and namespace qualifiers
            string nestedTypeName = ExtractBaseTypeName(m.Type);
            if (string.IsNullOrEmpty(nestedTypeName) || !typeLookup.ContainsKey(nestedTypeName))
            {
                sb.AppendLine(baseDesc);
                return;
            }

            // We have a complex type; append a colon (without double punctuation) and then nested members
            baseDesc =
                baseDesc.EndsWith('.') ? string.Concat(baseDesc.AsSpan(0, baseDesc.Length - 1), ":")
                : baseDesc.EndsWith(':') ? baseDesc
                : baseDesc + ":";
            sb.AppendLine(baseDesc);

            TypeInfoModel nested = typeLookup[nestedTypeName];
            // For arguments: include non-readonly; for outputs: include readonly
            List<MemberInfoModel> nestedMembers =
            [
                .. nested
                    .Members.Where(nm => isOutput ? nm.IsReadOnly : !nm.IsReadOnly)
                    .OrderBy(nm => nm.Name, StringComparer.Ordinal),
            ];

            foreach (MemberInfoModel nm in nestedMembers)
            {
                string line = isOutput
                    ? $"  - `{ToCamel(nm.Name)}` - {nm.Description}{FormatEnumSuffix(nm)}"
                    : $"  - `{ToCamel(nm.Name)}` - {(nm.IsRequired ? "(Required) " : "(Optional) ")}{nm.Description}{FormatEnumSuffix(nm)}";
                sb.AppendLine(line);
            }
        }

        private static string ExtractBaseTypeName(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return string.Empty;
            }

            string t = type.Trim();
            // remove nullable marker
            if (t.EndsWith('?'))
            {
                t = t[..^1];
            }
            // arrays
            if (t.EndsWith("[]", StringComparison.Ordinal))
            {
                t = t[..^2];
            }
            // generics: take inner type of List<T> or similar
            int lt = t.IndexOf('<');
            if (lt >= 0)
            {
                int gt = t.LastIndexOf('>');
                if (gt > lt)
                {
                    t = t.Substring(lt + 1, gt - lt - 1);
                }
            }
            // strip namespace qualifiers
            int lastDot = t.LastIndexOf('.');
            if (lastDot >= 0)
            {
                t = t[(lastDot + 1)..];
            }
            return t;
        }
    }
}
