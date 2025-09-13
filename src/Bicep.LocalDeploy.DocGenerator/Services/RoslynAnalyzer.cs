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
                        a.Name.ToString().IndexOf("ResourceType", StringComparison.Ordinal) >= 0
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

                // BicepDocMetadata("key","value") for front matter, allow multiple
                foreach (var attr in td.AttributeLists.SelectMany(a => a.Attributes))
                {
                    var attrName = attr.Name.ToString();
                    if (
                        attrName.IndexOf("BicepDocMetadata", StringComparison.Ordinal) >= 0
                        && attr.ArgumentList?.Arguments.Count >= 2
                    )
                    {
                        if (
                            attr.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax k
                            && attr.ArgumentList.Arguments[1].Expression
                                is LiteralExpressionSyntax v
                        )
                        {
                            var key = k.Token.ValueText;
                            var value = v.Token.ValueText;
                            if (
                                string.Equals(
                                    key,
                                    "Description",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                type.Summary = value;
                                if (options.Verbose)
                                {
                                    Console.WriteLine($"  Metadata: Description -> Summary");
                                }
                            }
                            else if (!type.FrontMatter.ContainsKey(key))
                            {
                                type.FrontMatter[key] = value;
                                if (options.Verbose)
                                {
                                    Console.WriteLine($"  Metadata: {key} = '{value}'");
                                }
                            }
                        }
                    }
                    else if (
                        attrName.IndexOf("BicepMetadata", StringComparison.Ordinal) >= 0
                        && attr.ArgumentList?.Arguments.Count >= 2
                    )
                    {
                        if (
                            attr.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax mk
                            && attr.ArgumentList.Arguments[1].Expression
                                is LiteralExpressionSyntax mv
                        )
                        {
                            var key = mk.Token.ValueText;
                            var value = mv.Token.ValueText;
                            // If key is Description, map to Summary; else store in front matter
                            if (
                                string.Equals(
                                    key,
                                    "Description",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                type.Summary = value;
                                if (options.Verbose)
                                {
                                    Console.WriteLine($"  Metadata: Description -> Summary");
                                }
                            }
                            else if (!type.FrontMatter.ContainsKey(key))
                            {
                                type.FrontMatter[key] = value;
                                if (options.Verbose)
                                {
                                    Console.WriteLine($"  Metadata: {key} = '{value}'");
                                }
                            }
                        }
                    }
                    else if (
                        attrName.IndexOf("BicepDocExample", StringComparison.Ordinal) >= 0
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
                        attrName.IndexOf("BicepDocCustom", StringComparison.Ordinal) >= 0
                        && attr.ArgumentList?.Arguments.Count >= 3
                    )
                    {
                        var args = attr.ArgumentList.Arguments;
                        if (
                            args[0].Expression is LiteralExpressionSyntax tt
                            && args[1].Expression is LiteralExpressionSyntax dd
                            && args[2].Expression is LiteralExpressionSyntax bb
                        )
                        {
                            var section = new CustomSectionModel
                            {
                                Title = tt.Token.ValueText,
                                Description = dd.Token.ValueText,
                                Body = bb.Token.ValueText,
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
                            a.Name.ToString().IndexOf("TypeProperty", StringComparison.Ordinal) >= 0
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
                            mi.IsRequired =
                                flagsText.IndexOf("Required", StringComparison.Ordinal) >= 0;
                            mi.IsReadOnly =
                                flagsText.IndexOf("ReadOnly", StringComparison.Ordinal) >= 0;
                            mi.IsIdentifier =
                                flagsText.IndexOf("Identifier", StringComparison.Ordinal) >= 0;
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
