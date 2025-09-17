using System.Globalization;
using System.Text;

namespace Bicep.LocalDeploy.DocGenerator.Services.Generation
{
    /// <summary>
    /// Generates reference sections for arguments and attributes in markdown documents.
    /// </summary>
    public static class ReferenceGenerator
    {
        /// <summary>
        /// Generates the argument reference section.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="requiredArgs">Required arguments to document.</param>
        /// <param name="optionalArgs">Optional arguments to document.</param>
        /// <param name="typeLookup">Lookup table for nested type resolution.</param>
        public static void GenerateArgumentReference(
            StringBuilder sb,
            List<MemberInfoModel> requiredArgs,
            List<MemberInfoModel> optionalArgs,
            IReadOnlyDictionary<string, TypeInfoModel> typeLookup
        )
        {
            sb.AppendLine("## Argument reference");
            sb.AppendLine();
            sb.AppendLine("The following arguments are available:");
            sb.AppendLine();

            foreach (MemberInfoModel member in requiredArgs)
            {
                AppendMemberWithNesting(sb, member, isOutput: false, typeLookup);
            }
            foreach (MemberInfoModel member in optionalArgs)
            {
                AppendMemberWithNesting(sb, member, isOutput: false, typeLookup);
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Generates the attribute reference section.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="outputs">Output attributes to document.</param>
        /// <param name="typeLookup">Lookup table for nested type resolution.</param>
        public static void GenerateAttributeReference(
            StringBuilder sb,
            List<MemberInfoModel> outputs,
            IReadOnlyDictionary<string, TypeInfoModel> typeLookup
        )
        {
            sb.AppendLine("## Attribute reference");
            sb.AppendLine();
            sb.AppendLine(
                "In addition to all arguments above, the following attributes are outputted:"
            );
            sb.AppendLine();

            foreach (MemberInfoModel member in outputs)
            {
                AppendMemberWithNesting(sb, member, isOutput: true, typeLookup);
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Generates custom sections from provided custom section models.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="customSections">The custom sections to generate.</param>
        public static void GenerateCustomSections(
            StringBuilder sb,
            List<CustomSectionModel> customSections
        )
        {
            foreach (CustomSectionModel section in customSections)
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

        /// <summary>
        /// Appends a member description with nested type support.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="member">The member to document.</param>
        /// <param name="isOutput">Whether this is an output attribute.</param>
        /// <param name="typeLookup">Lookup table for nested type resolution.</param>
        private static void AppendMemberWithNesting(
            StringBuilder sb,
            MemberInfoModel member,
            bool isOutput,
            IReadOnlyDictionary<string, TypeInfoModel> typeLookup
        )
        {
            // Base line
            string baseDesc = isOutput
                ? $"- `{ToCamel(member.Name)}` - {member.Description}{FormatEnumSuffix(member)}"
                : $"- `{ToCamel(member.Name)}` - {(member.IsRequired ? "(Required) " : "(Optional) ")}{member.Description}{FormatEnumSuffix(member)}";

            // Determine nested type by stripping nullable and generics/arrays and namespace qualifiers
            string nestedTypeName = ExtractBaseTypeName(member.Type);
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

            foreach (MemberInfoModel nestedMember in nestedMembers)
            {
                string line = isOutput
                    ? $"  - `{ToCamel(nestedMember.Name)}` - {nestedMember.Description}{FormatEnumSuffix(nestedMember)}"
                    : $"  - `{ToCamel(nestedMember.Name)}` - {(nestedMember.IsRequired ? "(Required) " : "(Optional) ")}{nestedMember.Description}{FormatEnumSuffix(nestedMember)}";
                sb.AppendLine(line);
            }
        }

        /// <summary>
        /// Formats enum values as a suffix for member descriptions.
        /// </summary>
        /// <param name="member">The member to format enum values for.</param>
        /// <returns>A formatted enum suffix string.</returns>
        private static string FormatEnumSuffix(MemberInfoModel member)
        {
            if (!member.IsEnum || member.EnumValues is null || member.EnumValues.Count == 0)
            {
                return string.Empty;
            }

            // Format values as: `A`, `B`, or `C`
            List<string> values = member.EnumValues;
            if (values.Count == 1)
            {
                return $" (Can be `{values[0]}`)";
            }

            string last = values[^1];
            IEnumerable<string> head = values.Take(values.Count - 1).Select(v => $"`{v}`");
            string joinedHead = string.Join(", ", head);
            return $" (Can be {joinedHead}, or `{last}`)";
        }

        /// <summary>
        /// Extracts the base type name from a complex type string.
        /// </summary>
        /// <param name="type">The type string to analyze.</param>
        /// <returns>The base type name.</returns>
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
