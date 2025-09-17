using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bicep.LocalDeploy.DocGenerator.Services.Analysis
{
    /// <summary>
    /// Analyzes enum declarations and maps enum values to type members.
    /// </summary>
    public static class EnumAnalyzer
    {
        /// <summary>
        /// Maps enum values to type members that reference those enums.
        /// </summary>
        /// <param name="enumDeclarations">The enum declarations found in source files.</param>
        /// <param name="types">The types to update with enum information.</param>
        /// <param name="verbose">Whether to output verbose logging.</param>
        public static void MapEnumValues(
            IEnumerable<EnumDeclarationSyntax> enumDeclarations,
            List<TypeInfoModel> types,
            bool verbose = false
        )
        {
            foreach (EnumDeclarationSyntax enumDecl in enumDeclarations)
            {
                string enumName = enumDecl.Identifier.ValueText;
                List<string> values = [.. enumDecl.Members.Select(m => m.Identifier.ValueText)];

                ApplyEnumValuesToTypes(enumName, values, types, verbose);
            }
        }

        /// <summary>
        /// Applies enum values to all type members that reference the specified enum.
        /// </summary>
        /// <param name="enumName">The name of the enum type.</param>
        /// <param name="enumValues">The possible values for the enum.</param>
        /// <param name="types">The types to update.</param>
        /// <param name="verbose">Whether to output verbose logging.</param>
        private static void ApplyEnumValuesToTypes(
            string enumName,
            List<string> enumValues,
            List<TypeInfoModel> types,
            bool verbose
        )
        {
            foreach (TypeInfoModel type in types)
            {
                foreach (
                    MemberInfoModel member in type.Members.Where(m =>
                        m.Type.Contains(enumName, StringComparison.Ordinal)
                    )
                )
                {
                    member.IsEnum = true;
                    member.EnumValues.Clear();
                    member.EnumValues.AddRange(enumValues);

                    if (verbose)
                    {
                        Console.WriteLine(
                            $"  Enum mapped: {type.Name}.{member.Name} -> {enumName} [{string.Join(", ", enumValues)}]"
                        );
                    }
                }
            }
        }
    }
}
