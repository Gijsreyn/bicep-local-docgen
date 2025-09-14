using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bicep.LocalDeploy.DocGenerator.Services
{
    /// <summary>
    /// Roslyn-based analyzer that scans C# sources and builds the documentation model.
    /// </summary>
    public sealed class RoslynAnalyzer
    {
        /// <summary>
        /// Analyze sources and return a structured model of types and attributes.
        /// </summary>
        /// <param name="options">Generation options controlling sources, patterns, and verbosity.</param>
        /// <returns>The populated <see cref="AnalysisResult"/>.</returns>
        public static async Task<AnalysisResult> AnalyzeAsync(GenerationOptions options)
        {
            AnalysisResult result = new();

            if (options.Verbose)
            {
                string srcs = string.Join(", ", options.SourceDirectories.Select(d => d.FullName));
                string pats = string.Join(", ", options.FilePatterns);
                Console.WriteLine($"Scanning sources: {srcs}");
                Console.WriteLine($"Patterns: {pats}");
            }

            List<FileInfo> files = [];
            foreach (DirectoryInfo dir in options.SourceDirectories)
            {
                if (!dir.Exists)
                {
                    continue;
                }

                foreach (string pattern in options.FilePatterns)
                {
                    string cleaned = pattern;
                    if (cleaned is [.., '\\'] or [.., '/'])
                    {
                        cleaned = Path.GetFileName(cleaned);
                    }

                    if (string.IsNullOrWhiteSpace(cleaned))
                    {
                        cleaned = "*.cs";
                    }

                    files.AddRange(dir.GetFiles(cleaned, SearchOption.AllDirectories));
                }
            }

            List<FileInfo> uniqueFiles =
            [
                .. files
                    .GroupBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First()),
            ];

            if (options.Verbose)
            {
                Console.WriteLine($"Found {uniqueFiles.Count} source file(s) to parse.");
            }

            foreach (FileInfo file in uniqueFiles)
            {
                if (options.Verbose)
                {
                    Console.WriteLine($"- Parsing {file.FullName}");
                }

                string text;
                using (StreamReader reader = new(file.FullName))
                {
                    text = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                SyntaxTree tree = CSharpSyntaxTree.ParseText(text, path: file.FullName);
                SyntaxNode root = await tree.GetRootAsync().ConfigureAwait(false);

                // classes and records
                List<TypeDeclarationSyntax> typeDecls =
                [
                    .. root.DescendantNodes()
                        .Where(n => n is ClassDeclarationSyntax or RecordDeclarationSyntax)
                        .Cast<TypeDeclarationSyntax>(),
                ];
                // enums
                List<EnumDeclarationSyntax> enumDecls =
                [
                    .. root.DescendantNodes().OfType<EnumDeclarationSyntax>(),
                ];

                foreach (TypeDeclarationSyntax td in typeDecls)
                {
                    string ns =
                        td.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>()?.Name.ToString()
                        ?? string.Empty;

                    TypeInfoModel type = new()
                    {
                        Name = td.Identifier.ValueText,
                        Namespace = ns,
                        SourceFile = file.FullName,
                    };

                    // ResourceType("Name")
                    AttributeSyntax? resourceTypeAttr = td
                        .AttributeLists.SelectMany(a => a.Attributes)
                        .FirstOrDefault(a =>
                            a.Name.ToString().Contains("ResourceType", StringComparison.Ordinal)
                        );

                    if (
                        resourceTypeAttr?.ArgumentList?.Arguments.FirstOrDefault()?.Expression
                        is LiteralExpressionSyntax lit
                    )
                    {
                        type.ResourceTypeName = lit.Token.ValueText;
                        if (options.Verbose)
                        {
                            Console.WriteLine(
                                $"  ResourceType: {type.ResourceTypeName} ({type.Name})"
                            );
                        }
                    }

                    // Attributes on the type
                    foreach (
                        AttributeSyntax attr in td.AttributeLists.SelectMany(a => a.Attributes)
                    )
                    {
                        string attrName = attr.Name.ToString();

                        if (
                            attrName.Contains("BicepFrontMatter", StringComparison.Ordinal)
                            && attr.ArgumentList?.Arguments.Count >= 2
                        )
                        {
                            SeparatedSyntaxList<AttributeArgumentSyntax> args =
                                attr.ArgumentList.Arguments;

                            // Determine target block index from named or positional argument
                            int blockIndex = 1;
                            AttributeArgumentSyntax? namedBlockIdx = args.FirstOrDefault(a =>
                                a.NameEquals?.Name.Identifier.ValueText == "BlockIndex"
                            );
                            if (
                                namedBlockIdx is not null
                                && namedBlockIdx.Expression is LiteralExpressionSyntax biLit
                                && int.TryParse(biLit.Token.ValueText, out int bi)
                            )
                            {
                                blockIndex = Math.Max(1, bi);
                            }
                            else if (
                                args.Count >= 3
                                && args[0].Expression is LiteralExpressionSyntax biPosLit
                                && int.TryParse(biPosLit.Token.ValueText, out int biPos)
                            )
                            {
                                blockIndex = Math.Max(1, biPos);
                            }

                            string? key = null;
                            string? value = null;

                            if (
                                args.Count == 2
                                && args[0].Expression is LiteralExpressionSyntax k1
                                && args[1].Expression is LiteralExpressionSyntax v1
                            )
                            {
                                key = k1.Token.ValueText;
                                value = v1.Token.ValueText;
                            }
                            else if (args.Count >= 3)
                            {
                                // take the last two as key/value
                                AttributeArgumentSyntax aKey = args[^2];
                                AttributeArgumentSyntax aVal = args[^1];
                                if (
                                    aKey.Expression is LiteralExpressionSyntax k2
                                    && aVal.Expression is LiteralExpressionSyntax v2
                                )
                                {
                                    key = k2.Token.ValueText;
                                    value = v2.Token.ValueText;
                                }
                            }

                            if (key is not null && value is not null)
                            {
                                while (type.FrontMatterBlocks.Count < blockIndex)
                                {
                                    type.FrontMatterBlocks.Add(new(StringComparer.Ordinal));
                                }

                                Dictionary<string, string> block = type.FrontMatterBlocks[
                                    blockIndex - 1
                                ];
                                if (!block.ContainsKey(key))
                                {
                                    block[key] = value;
                                    if (options.Verbose)
                                    {
                                        Console.WriteLine(
                                            blockIndex == 1
                                                ? $"  FrontMatter: {key} = '{value}'"
                                                : $"  FrontMatter[{blockIndex}]: {key} = '{value}'"
                                        );
                                    }
                                }
                            }
                        }
                        else if (
                            attrName.Contains("BicepDocHeading", StringComparison.Ordinal)
                            && attr.ArgumentList?.Arguments.Count >= 2
                        )
                        {
                            SeparatedSyntaxList<AttributeArgumentSyntax> args =
                                attr.ArgumentList.Arguments;
                            if (
                                args[0].Expression is LiteralExpressionSyntax ht
                                && args[1].Expression is LiteralExpressionSyntax hd
                            )
                            {
                                type.HeadingTitle = ht.Token.ValueText;
                                type.HeadingDescription = hd.Token.ValueText;
                                if (options.Verbose)
                                {
                                    Console.WriteLine($"  Heading: '{type.HeadingTitle}'");
                                }
                            }
                        }
                        else if (
                            attrName.Contains("BicepDocExample", StringComparison.Ordinal)
                            && attr.ArgumentList?.Arguments.Count >= 3
                        )
                        {
                            SeparatedSyntaxList<AttributeArgumentSyntax> args =
                                attr.ArgumentList.Arguments;
                            if (
                                args[0].Expression is LiteralExpressionSyntax t
                                && args[1].Expression is LiteralExpressionSyntax d
                                && args[2].Expression is LiteralExpressionSyntax c
                            )
                            {
                                ExampleModel example = new()
                                {
                                    Title = t.Token.ValueText,
                                    Description = d.Token.ValueText,
                                    Code = c.Token.ValueText,
                                    Language = "bicep",
                                };
                                if (
                                    args.Count >= 4
                                    && args[3].Expression is LiteralExpressionSyntax lang
                                )
                                {
                                    example.Language = lang.Token.ValueText;
                                }
                                type.Examples.Add(example);
                                if (options.Verbose)
                                {
                                    Console.WriteLine(
                                        $"  Example: '{example.Title}' (lang: {example.Language})"
                                    );
                                }
                            }
                        }
                        else if (
                            attrName.Contains("BicepDocCustom", StringComparison.Ordinal)
                            && attr.ArgumentList?.Arguments.Count >= 2
                        )
                        {
                            SeparatedSyntaxList<AttributeArgumentSyntax> args =
                                attr.ArgumentList.Arguments;
                            if (
                                args[0].Expression is LiteralExpressionSyntax tt
                                && args[1].Expression is LiteralExpressionSyntax dd
                            )
                            {
                                CustomSectionModel section = new()
                                {
                                    Title = tt.Token.ValueText,
                                    Description = dd.Token.ValueText,
                                };
                                type.CustomSections.Add(section);
                                if (options.Verbose)
                                {
                                    Console.WriteLine($"  Custom section: '{section.Title}'");
                                }
                            }
                        }
                    }

                    // base types
                    if (td.BaseList is not null)
                    {
                        foreach (BaseTypeSyntax bt in td.BaseList.Types)
                        {
                            string name = bt.Type.ToString().Split('<')[0].Split('.').Last();
                            type.BaseTypes.Add(name);
                        }
                        if (options.Verbose && type.BaseTypes.Count > 0)
                        {
                            Console.WriteLine($"  Base types: {string.Join(", ", type.BaseTypes)}");
                        }
                    }

                    // properties
                    foreach (
                        PropertyDeclarationSyntax prop in td.Members.OfType<PropertyDeclarationSyntax>()
                    )
                    {
                        if (!prop.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                        {
                            continue;
                        }

                        MemberInfoModel mi = new()
                        {
                            Name = prop.Identifier.ValueText,
                            Type = prop.Type.ToString(),
                        };

                        AttributeSyntax? typePropAttr = prop
                            .AttributeLists.SelectMany(a => a.Attributes)
                            .FirstOrDefault(a =>
                                a.Name.ToString().Contains("TypeProperty", StringComparison.Ordinal)
                            );

                        if (typePropAttr is not null)
                        {
                            if (
                                typePropAttr.ArgumentList?.Arguments.Count > 0
                                && typePropAttr.ArgumentList.Arguments[0].Expression
                                    is LiteralExpressionSyntax desc
                            )
                            {
                                mi.Description = desc.Token.ValueText;
                            }

                            if (typePropAttr.ArgumentList?.Arguments.Count > 1)
                            {
                                string flagsText = typePropAttr
                                    .ArgumentList.Arguments[1]
                                    .Expression.ToString();
                                mi.IsRequired = flagsText.Contains(
                                    "Required",
                                    StringComparison.Ordinal
                                );
                                mi.IsReadOnly = flagsText.Contains(
                                    "ReadOnly",
                                    StringComparison.Ordinal
                                );
                                mi.IsIdentifier = flagsText.Contains(
                                    "Identifier",
                                    StringComparison.Ordinal
                                );
                            }

                            if (options.Verbose)
                            {
                                List<string> flags = [];
                                if (mi.IsRequired)
                                {
                                    flags.Add("Required");
                                }
                                if (mi.IsReadOnly)
                                {
                                    flags.Add("ReadOnly");
                                }
                                if (mi.IsIdentifier)
                                {
                                    flags.Add("Identifier");
                                }
                                string flagsStr =
                                    flags.Count > 0
                                        ? $" [{string.Join(", ", flags)}]"
                                        : string.Empty;
                                Console.WriteLine($"  Property: {mi.Name} : {mi.Type}{flagsStr}");
                            }
                        }

                        // enum detection
                        mi.IsEnum = prop.Type is IdentifierNameSyntax or NullableTypeSyntax;

                        type.Members.Add(mi);
                    }

                    result.Types.Add(type);
                }

                // enums to populate enum values
                foreach (EnumDeclarationSyntax ed in enumDecls)
                {
                    string enumName = ed.Identifier.ValueText;
                    List<string> values = [.. ed.Members.Select(m => m.Identifier.ValueText)];
                    foreach (TypeInfoModel t in result.Types)
                    {
                        foreach (
                            MemberInfoModel m in t.Members.Where(m =>
                                m.Type.Contains(enumName, StringComparison.Ordinal)
                            )
                        )
                        {
                            m.IsEnum = true;
                            m.EnumValues.Clear();
                            m.EnumValues.AddRange(values);
                            if (options.Verbose)
                            {
                                Console.WriteLine(
                                    $"  Enum mapped: {t.Name}.{m.Name} -> {enumName} [{string.Join(", ", values)}]"
                                );
                            }
                        }
                    }
                }
            }

            // merge inherited members (single-level is sufficient for our case)
            Dictionary<string, TypeInfoModel> lookup = result.Types.ToDictionary(
                t => t.Name,
                t => t
            );
            foreach (TypeInfoModel t in result.Types)
            {
                foreach (string baseName in t.BaseTypes)
                {
                    if (lookup.TryGetValue(baseName, out TypeInfoModel? baseType))
                    {
                        foreach (MemberInfoModel bp in baseType.Members)
                        {
                            if (t.Members.Any(m => m.Name == bp.Name))
                            {
                                continue;
                            }

                            t.Members.Add(
                                new MemberInfoModel
                                {
                                    Name = bp.Name,
                                    Type = bp.Type,
                                    Description = bp.Description,
                                    IsRequired = bp.IsRequired,
                                    IsReadOnly = bp.IsReadOnly,
                                    IsIdentifier = bp.IsIdentifier,
                                    IsEnum = bp.IsEnum,
                                    EnumValues = [.. bp.EnumValues],
                                }
                            );
                        }
                    }
                }
            }

            return result;
        }
    }
}
