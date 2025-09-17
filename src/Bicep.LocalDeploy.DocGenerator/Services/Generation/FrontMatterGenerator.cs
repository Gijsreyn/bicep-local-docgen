using System.Globalization;
using System.Text;

namespace Bicep.LocalDeploy.DocGenerator.Services.Generation
{
    /// <summary>
    /// Generates YAML front matter sections for markdown documents.
    /// </summary>
    public static class FrontMatterGenerator
    {
        /// <summary>
        /// Generates YAML front matter blocks and appends them to the StringBuilder.
        /// </summary>
        /// <param name="sb">The StringBuilder to append to.</param>
        /// <param name="frontMatterBlocks">The front matter blocks to generate.</param>
        public static void Generate(
            StringBuilder sb,
            List<Dictionary<string, string>> frontMatterBlocks
        )
        {
            if (frontMatterBlocks.Count == 0)
            {
                return;
            }

            foreach (Dictionary<string, string> block in frontMatterBlocks)
            {
                sb.AppendLine("---");
                foreach (
                    KeyValuePair<string, string> kvp in block.OrderBy(
                        k => k.Key,
                        StringComparer.Ordinal
                    )
                )
                {
                    sb.AppendLine(
                        CultureInfo.InvariantCulture,
                        $"{ToKebab(kvp.Key)}: \"{kvp.Value}\""
                    );
                }
                sb.AppendLine("---");
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Retrieves a value from front matter blocks using case-insensitive key lookup.
        /// </summary>
        /// <param name="frontMatter">The front matter dictionary to search.</param>
        /// <param name="key">The key to search for.</param>
        /// <returns>The value if found, otherwise null.</returns>
        public static string? GetValue(IReadOnlyDictionary<string, string> frontMatter, string key)
        {
            // Case-insensitive lookup so either "title" or "Title" works
            foreach (KeyValuePair<string, string> kvp in frontMatter)
            {
                if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Converts a string to kebab-case for YAML front matter keys.
        /// </summary>
        /// <param name="name">The string to convert.</param>
        /// <returns>The kebab-case string.</returns>
        private static string ToKebab(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            StringBuilder sb = new(name.Length + 8);
            foreach (char ch in name)
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
    }
}
