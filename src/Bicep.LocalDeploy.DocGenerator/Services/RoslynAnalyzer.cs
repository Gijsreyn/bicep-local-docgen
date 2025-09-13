using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bicep.LocalDeploy.DocGenerator.Services;

/// <summary>
/// Uses Roslyn to parse C# and extract resource types and members based on attributes.
/// </summary>
public static class RoslynAnalyzer
{
    public static async Task<AnalysisResult> AnalyzeAsync(GenerationOptions options)
    {
        var result = new AnalysisResult();

        if (options.Verbose)
        {
            var srcs = string.Join(", ", options.SourceDirectories.Select(d => d.FullName));
            var pats = string.Join(", ", options.FilePatterns);
            Console.WriteLine($"Scanning sources: {srcs}");
            Console.WriteLine($"Patterns: {pats}");
        }

        var files = new List<FileInfo>();
        foreach (var dir in options.SourceDirectories)
        {
            if (!dir.Exists)
            {
                continue;
            }

            foreach (var pattern in options.FilePatterns)
            {
                var cleaned = pattern;
                if (cleaned.Contains('\\') || cleaned.Contains('/'))
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

        var uniqueFiles = files.GroupBy(f => f.FullName).Select(g => g.First()).ToList();

        if (options.Verbose)
        {
            Console.WriteLine($"Found {uniqueFiles.Count} source file(s) to parse.");
        }

        foreach (var file in uniqueFiles)
        {
            if (options.Verbose)
            {
                Console.WriteLine($"- Parsing {file.FullName}");
            }
            string text;
            using (var reader = new StreamReader(file.FullName))
            {
                text = await reader.ReadToEndAsync();
            }
            var tree = CSharpSyntaxTree.ParseText(text, path: file.FullName);
            var root = await tree.GetRootAsync();

            var ns =
                root.DescendantNodes()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .FirstOrDefault()
                    ?.Name.ToString()
                ?? string.Empty;

            // classes and records
            var typeDecls = root.DescendantNodes()
                .Where(n => n is ClassDeclarationSyntax || n is RecordDeclarationSyntax)
                .Cast<TypeDeclarationSyntax>()
                .ToList();
            // enums
            var enumDecls = root.DescendantNodes().OfType<EnumDeclarationSyntax>().ToList();

            foreach (var td in typeDecls)
            {
                TypeInfoModel type = new()
                {
                    Name = td.Identifier.ValueText,
                    Namespace = ns,
                    SourceFile = file.FullName,
                };

                // ResourceType("Name")
                var resourceTypeAttr = td
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
                        Console.WriteLine($"  ResourceType: {type.ResourceTypeName} ({type.Name})");
                    }
                }

                // BicepFrontMatter("key","value") and BicepFrontMatter(blockIndex:int, "key","value") for front matter blocks
                foreach (var attr in td.AttributeLists.SelectMany(a => a.Attributes))
                {
                    var attrName = attr.Name.ToString();
                    if (attrName.Contains("BicepFrontMatter", StringComparison.Ordinal)
                        && attr.ArgumentList?.Arguments.Count >= 2)
                    {
                        var args = attr.ArgumentList.Arguments;
                        // Form 1: (key, value)
                        if (args.Count == 2
                            && args[0].Expression is LiteralExpressionSyntax k1
                            && args[1].Expression is LiteralExpressionSyntax v1)
                        {
                            var key = k1.Token.ValueText;
                            var value = v1.Token.ValueText;
                            // Also mirror into block 1 of FrontMatterBlocks
                            if (type.FrontMatterBlocks.Count == 0)
                            {
                                type.FrontMatterBlocks.Add(new Dictionary<string, string>());
                            }
                            var block1 = type.FrontMatterBlocks[0];
                            if (!block1.ContainsKey(key))
                            {
                                block1[key] = value;
                                if (options.Verbose)
                                {
                                    Console.WriteLine($"  FrontMatter: {key} = '{value}'");
                                }
                            }
                        }
                        // Form 2: (blockIndex:int, key, value)
                        else if (args.Count >= 3
                            && args[0].Expression is LiteralExpressionSyntax b
                            && int.TryParse(b.Token.ValueText, out var blockIndex)
                            && args[1].Expression is LiteralExpressionSyntax k2
                            && args[2].Expression is LiteralExpressionSyntax v2)
                        {
                            if (blockIndex < 1)
                            {
                                blockIndex = 1;
                            }
                            var key = k2.Token.ValueText;
                            var value = v2.Token.ValueText;
                            // Ensure list size
                            while (type.FrontMatterBlocks.Count < blockIndex)
                            {
                                type.FrontMatterBlocks.Add(new Dictionary<string, string>());
                            }
                            var block = type.FrontMatterBlocks[blockIndex - 1];
                            if (!block.ContainsKey(key))
                            {
                                block[key] = value;
                                if (options.Verbose)
                                {
                                    Console.WriteLine($"  FrontMatter[{blockIndex}]: {key} = '{value}'");
                                }
                            }
                        }
                    }
                    else if (attrName.Contains("BicepDocHeading", StringComparison.Ordinal)
                             && attr.ArgumentList?.Arguments.Count >= 2)
                    {
                        var args = attr.ArgumentList.Arguments;
                        if (args[0].Expression is LiteralExpressionSyntax ht
                            && args[1].Expression is LiteralExpressionSyntax hd)
                        {
                            type.HeadingTitle = ht.Token.ValueText;
                            type.HeadingDescription = hd.Token.ValueText;
                            if (options.Verbose)
                            {
                                Console.WriteLine($"  Heading: '{type.HeadingTitle}'");
                            }
                        }
                    }
                    // No else: the legacy BicepMetadata/BicepDocMetadata are removed.
                    else if (
                        attrName.Contains("BicepDocExample", StringComparison.Ordinal)
                        && attr.ArgumentList?.Arguments.Count >= 3
                    )
                    {
                        var args = attr.ArgumentList.Arguments;
                        if (
                            args[0].Expression is LiteralExpressionSyntax t
                            && args[1].Expression is LiteralExpressionSyntax d
                            && args[2].Expression is LiteralExpressionSyntax c
                        )
                        {
                            var example = new ExampleModel
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
                        var args = attr.ArgumentList.Arguments;
                        if (
                            args[0].Expression is LiteralExpressionSyntax tt
                            && args[1].Expression is LiteralExpressionSyntax dd
                        )
                        {
                            var section = new CustomSectionModel
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
                    foreach (var bt in td.BaseList.Types)
                    {
                        var name = bt.Type.ToString().Split('<')[0].Split('.').Last();
                        type.BaseTypes.Add(name);
                    }
                    if (options.Verbose && type.BaseTypes.Count > 0)
                    {
                        Console.WriteLine($"  Base types: {string.Join(", ", type.BaseTypes)}");
                    }
                }

                // properties
                foreach (var prop in td.Members.OfType<PropertyDeclarationSyntax>())
                {
                    if (!prop.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                    {
                        continue;
                    }

                    var mi = new MemberInfoModel
                    {
                        Name = prop.Identifier.ValueText,
                        Type = prop.Type.ToString(),
                    };

                    var typePropAttr = prop
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
                            var flagsText = typePropAttr
                                .ArgumentList.Arguments[1]
                                .Expression.ToString();
                            mi.IsRequired = flagsText.Contains("Required", StringComparison.Ordinal);
                            mi.IsReadOnly = flagsText.Contains("ReadOnly", StringComparison.Ordinal);
                            mi.IsIdentifier = flagsText.Contains("Identifier", StringComparison.Ordinal);
                        }
                        if (options.Verbose)
                        {
                            var flags = new List<string>();
                            if (mi.IsRequired)
                                flags.Add("Required");
                            if (mi.IsReadOnly)
                                flags.Add("ReadOnly");
                            if (mi.IsIdentifier)
                                flags.Add("Identifier");
                            var flagsStr =
                                flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : string.Empty;
                            Console.WriteLine($"  Property: {mi.Name} : {mi.Type}{flagsStr}");
                        }
                    }

                    // enum detection
                    mi.IsEnum =
                        prop.Type is IdentifierNameSyntax || prop.Type is NullableTypeSyntax;

                    type.Members.Add(mi);
                }

                result.Types.Add(type);
            }

            // enums to populate enum values
            foreach (var ed in enumDecls)
            {
                var enumName = ed.Identifier.ValueText;
                var values = ed.Members.Select(m => m.Identifier.ValueText).ToList();
                foreach (var t in result.Types)
                {
                    foreach (var m in t.Members.Where(m => m.Type.Contains(enumName)))
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
        var lookup = result.Types.ToDictionary(t => t.Name, t => t);
        foreach (var t in result.Types)
        {
            foreach (var baseName in t.BaseTypes)
            {
                if (lookup.TryGetValue(baseName, out var baseType))
                {
                    foreach (var bp in baseType.Members)
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
                                EnumValues = new List<string>(bp.EnumValues),
                            }
                        );
                    }
                }
            }
        }

        return result;
    }
}
