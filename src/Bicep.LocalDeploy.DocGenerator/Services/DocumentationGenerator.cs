using System.Linq;
using System.Text;

namespace Bicep.LocalDeploy.DocGenerator.Services;

public sealed class DocumentationGenerator
{
    public async Task GenerateAsync(GenerationOptions options)
    {
        if (!options.OutputDirectory.Exists)
        {
            options.OutputDirectory.Create();
        }

        var analysis = await RoslynAnalyzer.AnalyzeAsync(options);

        // Only consider types with ResourceType attribute
        var resources = analysis.Types.Where(t => t.ResourceTypeName is not null).ToList();

        if (options.Verbose)
        {
            Console.WriteLine(
                $"Generating docs for {resources.Count} resource type(s) to '{options.OutputDirectory.FullName}'..."
            );
        }

        // Build lookup for type nesting
        var typeLookup = analysis.Types.ToDictionary(t => t.Name, t => t);

        foreach (var type in resources)
        {
            var md = GenerateMarkdownForType(type, typeLookup);
            var name = (type.ResourceTypeName ?? type.Name).ToLowerInvariant();
            var file = Path.Combine(options.OutputDirectory.FullName, $"{name}.md");
            var exists = File.Exists(file);

            if (exists && !options.Force)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"Warning: {file} already exists. Skipping. Use --force to overwrite."
                );
                Console.ResetColor();
                continue;
            }

            var mode = exists ? FileMode.Truncate : FileMode.Create;
            using var stream = new FileStream(
                file,
                mode,
                FileAccess.Write,
                FileShare.None,
                4096,
                useAsync: true
            );
            using var writer = new StreamWriter(stream, Encoding.UTF8);
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
        var resourceName = type.ResourceTypeName ?? type.Name;

        var requiredArgs = type
            .Members.Where(m => m.IsRequired && !m.IsReadOnly)
            .OrderBy(m => m.Name)
            .ToList();

        var optionalArgs = type
            .Members.Where(m => !m.IsRequired && !m.IsReadOnly)
            .OrderBy(m => m.Name)
            .ToList();

        var outputs = type.Members.Where(m => m.IsReadOnly).OrderBy(m => m.Name).ToList();

        var sb = new StringBuilder();
        // YAML front matter if any
        if (type.FrontMatter.Count > 0)
        {
            sb.AppendLine("---");
            foreach (var kvp in type.FrontMatter.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                sb.AppendLine($"{ToKebab(kvp.Key)}: \"{kvp.Value}\"");
            }
            sb.AppendLine("---");
            sb.AppendLine();
        }

        sb.AppendLine($"# {type.ResourceTypeName ?? "<handlerName>"}");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(type.Summary))
        {
            sb.AppendLine(type.Summary);
        }
        else
        {
            sb.AppendLine($"Manages {resourceName} resources.");
        }
        sb.AppendLine();

        // Examples: if custom examples provided via attributes, render those; else use defaults
        if (type.Examples.Count > 0)
        {
            sb.AppendLine("## Example usage");
            sb.AppendLine();
            foreach (var ex in type.Examples)
            {
                if (!string.IsNullOrWhiteSpace(ex.Title))
                {
                    sb.AppendLine($"### {ex.Title}");
                    sb.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(ex.Description))
                {
                    sb.AppendLine(ex.Description);
                    sb.AppendLine();
                }
                var lang = string.IsNullOrWhiteSpace(ex.Language) ? "bicep" : ex.Language;
                sb.AppendLine($"```{lang}");
                var code = (ex.Code ?? string.Empty).Trim('\r', '\n');
                sb.Append(code);
                if (code.Length > 0)
                {
                    var last = code[code.Length - 1];
                    if (last != '\n' && last != '\r')
                    {
                        sb.AppendLine();
                    }
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

            foreach (var m in requiredArgs)
            {
                AppendMemberWithNesting(sb, m, isOutput: false, typeLookup: typeLookup);
            }
            foreach (var m in optionalArgs)
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

            foreach (var m in outputs)
            {
                AppendMemberWithNesting(sb, m, isOutput: true, typeLookup: typeLookup);
            }

            sb.AppendLine();
        }

        // Custom sections (if any) appended at the end in chronological order
        if (type.CustomSections.Count > 0)
        {
            foreach (var section in type.CustomSections)
            {
                if (!string.IsNullOrWhiteSpace(section.Title))
                {
                    sb.AppendLine($"## {section.Title}");
                    sb.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(section.Description))
                {
                    sb.AppendLine(section.Description);
                    sb.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(section.Body))
                {
                    sb.AppendLine(section.Body);
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString();
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
        sb.AppendLine($"### Basic {resourceName}");
        sb.AppendLine();
        sb.AppendLine($"Creating a basic {resourceName} resource:");
        sb.AppendLine();
        sb.AppendLine("```bicep");
        sb.Append(GenerateBicep(resourceName, requiredArgs));
        sb.AppendLine("```");
        sb.AppendLine();

        // If there are optional args, add an advanced example
        if (optionalArgs.Count > 0)
        {
            sb.AppendLine($"### Advanced {resourceName}");
            sb.AppendLine();
            sb.AppendLine($"Creating a {resourceName} resource with optional settings:");
            sb.AppendLine();
            sb.AppendLine("```bicep");
            var all = new List<MemberInfoModel>();
            all.AddRange(requiredArgs);
            all.AddRange(optionalArgs);
            sb.Append(GenerateBicep(resourceName, all));
            sb.AppendLine("```");
            sb.AppendLine();
        }
    }

    private static string GenerateBicep(string resourceName, List<MemberInfoModel> props)
    {
        var varName = ToCamel(resourceName);
        var sb = new StringBuilder();
        sb.AppendLine($"resource {varName} '{resourceName}' = {{");
        foreach (var p in props)
        {
            sb.AppendLine($"  {ToCamel(p.Name)}: {ExampleFor(p)}");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string ExampleFor(MemberInfoModel p)
    {
        var type = p.Type.TrimEnd('?');
        return type switch
        {
            "string" or "String" => p.Name.IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0
                ? "'/Path/example/test'"
                : "'example'",
            "bool" or "Boolean" => "true",
            "int" or "Int32" or "long" or "Int64" => "1",
            "double" or "float" or "Single" or "Decimal" or "decimal" => "1.0",
            _ when type.IndexOf("[]", StringComparison.Ordinal) >= 0
                    || type.IndexOf("List<", StringComparison.Ordinal) >= 0 => "[]",
            _ when p.IsEnum && p.EnumValues.Count > 0 => $"'{p.EnumValues.First()}'",
            _ => "{}",
        };
    }

    private static string ToCamel(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string ToKebab(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        var sb = new StringBuilder(name.Length + 8);
        foreach (var ch in name)
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
        var values = m.EnumValues;
        if (values.Count == 1)
        {
            return $" (Can be `{values[0]}`)";
        }

        var last = values[values.Count - 1];
        var head = values.Take(values.Count - 1).Select(v => $"`{v}`");
        var joinedHead = string.Join(", ", head);
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
        var baseDesc = isOutput
            ? $"- `{ToCamel(m.Name)}` - {m.Description}{FormatEnumSuffix(m)}"
            : $"- `{ToCamel(m.Name)}` - {(m.IsRequired ? "(Required) " : "(Optional) ")}{m.Description}{FormatEnumSuffix(m)}";

        // Determine nested type by stripping nullable and generics/arrays and namespace qualifiers
        var nestedTypeName = ExtractBaseTypeName(m.Type);
        if (string.IsNullOrEmpty(nestedTypeName) || !typeLookup.ContainsKey(nestedTypeName))
        {
            sb.AppendLine(baseDesc);
            return;
        }

        // We have a complex type; append a colon (without double punctuation) and then nested members
        if (baseDesc.EndsWith(".", StringComparison.Ordinal))
        {
            baseDesc = baseDesc.Substring(0, baseDesc.Length - 1) + ":";
        }
        else if (!baseDesc.EndsWith(":", StringComparison.Ordinal))
        {
            baseDesc += ":";
        }
        sb.AppendLine(baseDesc);

        var nested = typeLookup[nestedTypeName];
        // For arguments: include non-readonly; for outputs: include readonly
        var nestedMembers = nested
            .Members.Where(nm => isOutput ? nm.IsReadOnly : !nm.IsReadOnly)
            .OrderBy(nm => nm.Name, StringComparer.Ordinal)
            .ToList();

        foreach (var nm in nestedMembers)
        {
            if (isOutput)
            {
                sb.AppendLine($"  - `{ToCamel(nm.Name)}` - {nm.Description}{FormatEnumSuffix(nm)}");
            }
            else
            {
                sb.AppendLine(
                    $"  - `{ToCamel(nm.Name)}` - {(nm.IsRequired ? "(Required) " : "(Optional) ")}{nm.Description}{FormatEnumSuffix(nm)}"
                );
            }
        }
    }

    private static string ExtractBaseTypeName(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return string.Empty;
        }

        var t = type.Trim();
        // remove nullable marker
        if (t.EndsWith("?", StringComparison.Ordinal))
            t = t.Substring(0, t.Length - 1);
        // arrays
        if (t.EndsWith("[]", StringComparison.Ordinal))
            t = t.Substring(0, t.Length - 2);
        // generics: take inner type of List<T> or similar
        var lt = t.IndexOf('<');
        if (lt >= 0)
        {
            var gt = t.LastIndexOf('>');
            if (gt > lt)
            {
                t = t.Substring(lt + 1, gt - lt - 1);
            }
        }
        // strip namespace qualifiers
        var lastDot = t.LastIndexOf('.');
        if (lastDot >= 0)
        {
            t = t.Substring(lastDot + 1);
        }
        return t;
    }
}
