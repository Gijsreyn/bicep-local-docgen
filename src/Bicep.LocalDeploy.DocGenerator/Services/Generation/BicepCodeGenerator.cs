using System.Globalization;
using System.Text;

namespace Bicep.LocalDeploy.DocGenerator.Services.Generation
{
    /// <summary>
    /// Generates Bicep code examples for resource definitions.
    /// </summary>
    public static class BicepCodeGenerator
    {
        /// <summary>
        /// Generates a Bicep resource definition with the specified properties.
        /// </summary>
        /// <param name="resourceName">The name of the resource type.</param>
        /// <param name="properties">The properties to include in the resource definition.</param>
        /// <returns>A formatted Bicep resource definition string.</returns>
        public static string Generate(string resourceName, List<MemberInfoModel> properties)
        {
            string varName = ToCamel(resourceName);
            StringBuilder sb = new();
            sb.AppendLine(
                CultureInfo.InvariantCulture,
                $"resource {varName} '{resourceName}' = {{"
            );

            foreach (MemberInfoModel property in properties)
            {
                sb.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"  {ToCamel(property.Name)}: {GenerateExampleValue(property)}"
                );
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Generates an appropriate example value for a property based on its type.
        /// </summary>
        /// <param name="property">The property to generate an example value for.</param>
        /// <returns>A string representation of an example value.</returns>
        private static string GenerateExampleValue(MemberInfoModel property)
        {
            string type = property.Type.TrimEnd('?');
            return type switch
            {
                "string" or "String" => property.Name.Contains(
                    "path",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "'/Path/example/test'"
                    : "'example'",
                "bool" or "Boolean" => "true",
                "int" or "Int32" or "long" or "Int64" => "1",
                "double" or "float" or "Single" or "Decimal" or "decimal" => "1.0",
                _ when type.Contains("[]", StringComparison.Ordinal)
                        || type.Contains("List<", StringComparison.Ordinal) => "[]",
                _ when property.IsEnum && property.EnumValues.Count > 0 =>
                    $"'{property.EnumValues.First()}'",
                _ => "{}",
            };
        }

        /// <summary>
        /// Converts a string to camelCase.
        /// </summary>
        /// <param name="name">The string to convert.</param>
        /// <returns>The camelCase string.</returns>
        private static string ToCamel(string name)
        {
            return string.IsNullOrEmpty(name)
                ? name
                : char.ToLowerInvariant(name[0]) + name.AsSpan(1).ToString();
        }
    }
}
