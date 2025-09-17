namespace Bicep.LocalDeploy.DocGenerator.Services.Analysis
{
    /// <summary>
    /// Handles type inheritance resolution and member merging for analyzed types.
    /// </summary>
    public static class InheritanceResolver
    {
        /// <summary>
        /// Merges inherited members from base types into derived types.
        /// Performs single-level inheritance resolution which is sufficient for most Bicep models.
        /// </summary>
        /// <param name="types">The list of types to process for inheritance.</param>
        /// <param name="verbose">Whether to output verbose logging.</param>
        public static void ResolveInheritance(List<TypeInfoModel> types, bool verbose = false)
        {
            // Build lookup for efficient type resolution
            Dictionary<string, TypeInfoModel> lookup = types
                .GroupBy(t => t.Name)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (TypeInfoModel type in types)
            {
                foreach (string baseName in type.BaseTypes)
                {
                    if (!lookup.TryGetValue(baseName, out TypeInfoModel? baseType))
                    {
                        if (verbose)
                        {
                            Console.WriteLine(
                                $"  Warning: Base type '{baseName}' not found for '{type.Name}'"
                            );
                        }
                        continue;
                    }

                    MergeBaseTypeMembers(type, baseType, verbose);
                }
            }
        }

        /// <summary>
        /// Merges members from a base type into a derived type, avoiding duplicates.
        /// </summary>
        /// <param name="derivedType">The derived type to merge members into.</param>
        /// <param name="baseType">The base type to merge members from.</param>
        /// <param name="verbose">Whether to output verbose logging.</param>
        private static void MergeBaseTypeMembers(
            TypeInfoModel derivedType,
            TypeInfoModel baseType,
            bool verbose
        )
        {
            int mergedCount = 0;

            foreach (MemberInfoModel baseMember in baseType.Members)
            {
                // Skip if derived type already has a member with this name (override scenario)
                if (derivedType.Members.Any(m => m.Name == baseMember.Name))
                {
                    if (verbose)
                    {
                        Console.WriteLine(
                            $"  Skipping inherited member '{baseMember.Name}' - overridden in '{derivedType.Name}'"
                        );
                    }
                    continue;
                }

                // Create a copy of the base member to avoid reference sharing
                MemberInfoModel inheritedMember = new()
                {
                    Name = baseMember.Name,
                    Type = baseMember.Type,
                    Description = baseMember.Description,
                    IsRequired = baseMember.IsRequired,
                    IsReadOnly = baseMember.IsReadOnly,
                    IsIdentifier = baseMember.IsIdentifier,
                    IsEnum = baseMember.IsEnum,
                    EnumValues = [.. baseMember.EnumValues],
                };

                derivedType.Members.Add(inheritedMember);
                mergedCount++;
            }

            if (verbose && mergedCount > 0)
            {
                Console.WriteLine(
                    $"  Inherited {mergedCount} member(s) from '{baseType.Name}' into '{derivedType.Name}'"
                );
            }
        }

        /// <summary>
        /// Extracts base type names from a type declaration's base list.
        /// </summary>
        /// <param name="typeDeclaration">The type declaration to analyze.</param>
        /// <param name="verbose">Whether to output verbose logging.</param>
        /// <returns>List of base type names.</returns>
        public static List<string> ExtractBaseTypes(
            Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax typeDeclaration,
            bool verbose = false
        )
        {
            List<string> baseTypes = [];

            if (typeDeclaration.BaseList is not null)
            {
                foreach (
                    Microsoft.CodeAnalysis.CSharp.Syntax.BaseTypeSyntax bt in typeDeclaration
                        .BaseList
                        .Types
                )
                {
                    string name = bt.Type.ToString().Split('<')[0].Split('.').Last();
                    baseTypes.Add(name);
                }

                if (verbose && baseTypes.Count > 0)
                {
                    Console.WriteLine($"  Base types: {string.Join(", ", baseTypes)}");
                }
            }

            return baseTypes;
        }
    }
}
