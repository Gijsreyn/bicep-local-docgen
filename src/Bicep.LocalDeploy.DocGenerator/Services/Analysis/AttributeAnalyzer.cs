using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bicep.LocalDeploy.DocGenerator.Services.Analysis
{
    /// <summary>
    /// Analyzes and extracts information from Bicep-specific attributes on types and members.
    /// </summary>
    public static class AttributeAnalyzer
    {
        /// <summary>
        /// Extracts ResourceType attribute value from a type declaration.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration to analyze.</param>
        /// <returns>The resource type name if found, otherwise null.</returns>
        public static string? ExtractResourceTypeName(TypeDeclarationSyntax typeDeclaration)
        {
            AttributeSyntax? resourceTypeAttr = typeDeclaration
                .AttributeLists.SelectMany(a => a.Attributes)
                .FirstOrDefault(a =>
                    a.Name.ToString().Contains("ResourceType", StringComparison.Ordinal)
                );

            return resourceTypeAttr?.ArgumentList?.Arguments.FirstOrDefault()?.Expression
                is LiteralExpressionSyntax lit
                ? lit.Token.ValueText
                : (string?)null;
        }

        /// <summary>
        /// Extracts all BicepFrontMatter attributes from a type declaration.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration to analyze.</param>
        /// <param name="verbose">Whether to output verbose logging.</param>
        /// <returns>List of front matter blocks with their key-value pairs.</returns>
        public static List<Dictionary<string, string>> ExtractFrontMatterBlocks(
            TypeDeclarationSyntax typeDeclaration,
            bool verbose = false
        )
        {
            List<Dictionary<string, string>> frontMatterBlocks = [];

            foreach (
                AttributeSyntax attr in typeDeclaration.AttributeLists.SelectMany(a => a.Attributes)
            )
            {
                string attrName = attr.Name.ToString();

                if (
                    !attrName.Contains("BicepFrontMatter", StringComparison.Ordinal)
                    || attr.ArgumentList?.Arguments.Count < 2
                )
                {
                    continue;
                }

                SeparatedSyntaxList<AttributeArgumentSyntax> args = attr.ArgumentList!.Arguments; // Determine target block index from named or positional argument
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
                    while (frontMatterBlocks.Count < blockIndex)
                    {
                        frontMatterBlocks.Add(new(StringComparer.Ordinal));
                    }

                    Dictionary<string, string> block = frontMatterBlocks[blockIndex - 1];
                    if (!block.ContainsKey(key))
                    {
                        block[key] = value;
                        if (verbose)
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

            return frontMatterBlocks;
        }

        /// <summary>
        /// Extracts BicepDocHeading attribute information from a type declaration.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration to analyze.</param>
        /// <param name="verbose">Whether to output verbose logging.</param>
        /// <returns>Tuple of (title, description) if found, otherwise (null, null).</returns>
        public static (string? Title, string? Description) ExtractHeadingInfo(
            TypeDeclarationSyntax typeDeclaration,
            bool verbose = false
        )
        {
            AttributeSyntax? headingAttr = typeDeclaration
                .AttributeLists.SelectMany(a => a.Attributes)
                .FirstOrDefault(a =>
                    a.Name.ToString().Contains("BicepDocHeading", StringComparison.Ordinal)
                );

            if (headingAttr?.ArgumentList?.Arguments.Count >= 2)
            {
                SeparatedSyntaxList<AttributeArgumentSyntax> args = headingAttr
                    .ArgumentList
                    .Arguments;
                if (
                    args[0].Expression is LiteralExpressionSyntax ht
                    && args[1].Expression is LiteralExpressionSyntax hd
                )
                {
                    string title = ht.Token.ValueText;
                    string description = hd.Token.ValueText;

                    if (verbose)
                    {
                        Console.WriteLine($"  Heading: '{title}'");
                    }

                    return (title, description);
                }
            }

            return (null, null);
        }

        /// <summary>
        /// Extracts all BicepDocExample attributes from a type declaration.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration to analyze.</param>
        /// <param name="verbose">Whether to output verbose logging.</param>
        /// <returns>List of example models.</returns>
        public static List<ExampleModel> ExtractExamples(
            TypeDeclarationSyntax typeDeclaration,
            bool verbose = false
        )
        {
            List<ExampleModel> examples = [];

            foreach (
                AttributeSyntax attr in typeDeclaration.AttributeLists.SelectMany(a => a.Attributes)
            )
            {
                string attrName = attr.Name.ToString();

                if (
                    !attrName.Contains("BicepDocExample", StringComparison.Ordinal)
                    || attr.ArgumentList?.Arguments.Count < 3
                )
                {
                    continue;
                }

                SeparatedSyntaxList<AttributeArgumentSyntax> args = attr.ArgumentList!.Arguments;
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

                    if (args.Count >= 4 && args[3].Expression is LiteralExpressionSyntax lang)
                    {
                        example.Language = lang.Token.ValueText;
                    }

                    examples.Add(example);

                    if (verbose)
                    {
                        Console.WriteLine(
                            $"  Example: '{example.Title}' (lang: {example.Language})"
                        );
                    }
                }
            }

            return examples;
        }

        /// <summary>
        /// Extracts all BicepDocCustom attributes from a type declaration.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration to analyze.</param>
        /// <param name="verbose">Whether to output verbose logging.</param>
        /// <returns>List of custom section models.</returns>
        public static List<CustomSectionModel> ExtractCustomSections(
            TypeDeclarationSyntax typeDeclaration,
            bool verbose = false
        )
        {
            List<CustomSectionModel> customSections = [];

            foreach (
                AttributeSyntax attr in typeDeclaration.AttributeLists.SelectMany(a => a.Attributes)
            )
            {
                string attrName = attr.Name.ToString();

                if (
                    !attrName.Contains("BicepDocCustom", StringComparison.Ordinal)
                    || attr.ArgumentList?.Arguments.Count < 2
                )
                {
                    continue;
                }

                SeparatedSyntaxList<AttributeArgumentSyntax> args = attr.ArgumentList!.Arguments;
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

                    customSections.Add(section);

                    if (verbose)
                    {
                        Console.WriteLine($"  Custom section: '{section.Title}'");
                    }
                }
            }

            return customSections;
        }

        /// <summary>
        /// Extracts TypeProperty attribute information from a property declaration.
        /// </summary>
        /// <param name="property">The property declaration to analyze.</param>
        /// <param name="verbose">Whether to output verbose logging.</param>
        /// <returns>Member info model with extracted data.</returns>
        public static MemberInfoModel ExtractPropertyInfo(
            PropertyDeclarationSyntax property,
            bool verbose = false
        )
        {
            MemberInfoModel mi = new()
            {
                Name = property.Identifier.ValueText,
                Type = property.Type.ToString(),
            };

            AttributeSyntax? typePropAttr = property
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
                    string flagsText = typePropAttr.ArgumentList.Arguments[1].Expression.ToString();
                    mi.IsRequired = flagsText.Contains("Required", StringComparison.Ordinal);
                    mi.IsReadOnly = flagsText.Contains("ReadOnly", StringComparison.Ordinal);
                    mi.IsIdentifier = flagsText.Contains("Identifier", StringComparison.Ordinal);
                }

                if (verbose)
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
                        flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : string.Empty;
                    Console.WriteLine($"  Property: {mi.Name} : {mi.Type}{flagsStr}");
                }
            }

            // enum detection
            mi.IsEnum = property.Type is IdentifierNameSyntax or NullableTypeSyntax;

            return mi;
        }
    }
}
