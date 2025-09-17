using Bicep.LocalDeploy.DocGenerator.Services.Analysis;
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

            // Load ignore file from current directory, not source directory
            string ignoreBaseDirectory = Directory.GetCurrentDirectory();
            IgnoreFile ignoreFile = await IgnoreFile.CreateAsync(ignoreBaseDirectory, options.IgnorePath);

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
                    .Select(g => g.First())
                    .Where(f => !ignoreFile.IsIgnored(f.FullName)),
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
                    string ns = td.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>()?.Name.ToString() ?? string.Empty;

                    TypeInfoModel type = new()
                    {
                        Name = td.Identifier.ValueText,
                        Namespace = ns,
                        SourceFile = file.FullName,
                        // Extract ResourceType attribute
                        ResourceTypeName = AttributeAnalyzer.ExtractResourceTypeName(td)
                    };
                    if (type.ResourceTypeName is not null && options.Verbose)
                    {
                        Console.WriteLine($"  ResourceType: {type.ResourceTypeName} ({type.Name})");
                    }

                    // Extract all Bicep-specific attributes
                    List<Dictionary<string, string>> frontMatterBlocks =
                        AttributeAnalyzer.ExtractFrontMatterBlocks(td, options.Verbose);
                    foreach (Dictionary<string, string> block in frontMatterBlocks)
                    {
                        type.FrontMatterBlocks.Add(block);
                    }

                    (type.HeadingTitle, type.HeadingDescription) =
                        AttributeAnalyzer.ExtractHeadingInfo(td, options.Verbose);

                    List<ExampleModel> examples = AttributeAnalyzer.ExtractExamples(
                        td,
                        options.Verbose
                    );
                    foreach (ExampleModel example in examples)
                    {
                        type.Examples.Add(example);
                    }

                    List<CustomSectionModel> customSections =
                        AttributeAnalyzer.ExtractCustomSections(td, options.Verbose);
                    foreach (CustomSectionModel section in customSections)
                    {
                        type.CustomSections.Add(section);
                    }

                    // Extract base types
                    List<string> baseTypes = InheritanceResolver.ExtractBaseTypes(
                        td,
                        options.Verbose
                    );
                    foreach (string baseType in baseTypes)
                    {
                        type.BaseTypes.Add(baseType);
                    }

                    // Extract properties
                    foreach (
                        PropertyDeclarationSyntax prop in td.Members.OfType<PropertyDeclarationSyntax>()
                    )
                    {
                        if (!prop.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                        {
                            continue;
                        }

                        MemberInfoModel memberInfo = AttributeAnalyzer.ExtractPropertyInfo(
                            prop,
                            options.Verbose
                        );
                        type.Members.Add(memberInfo);
                    }

                    result.Types.Add(type);
                }

                // Map enum values to types
                EnumAnalyzer.MapEnumValues(enumDecls, result.Types, options.Verbose);
            }

            // Resolve inheritance relationships
            InheritanceResolver.ResolveInheritance(result.Types, options.Verbose);

            return result;
        }
    }
}
