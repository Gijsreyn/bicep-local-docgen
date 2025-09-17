using System.Text.RegularExpressions;

namespace Bicep.LocalDeploy.DocGenerator.Services
{
    /// <summary>
    /// Handles ignore file patterns for excluding files from processing.
    /// </summary>
    internal sealed class IgnoreFile
    {
        private readonly List<Regex> _compiledPatterns;

        private IgnoreFile(List<string> patterns)
        {
            _compiledPatterns = CompilePatterns(patterns);
        }

        /// <summary>
        /// Creates an ignore file instance from the specified path or default location.
        /// </summary>
        /// <param name="baseDirectory">Base directory to search for ignore files.</param>
        /// <param name="ignorePath">Optional explicit path to ignore file.</param>
        /// <returns>IgnoreFile instance with loaded patterns.</returns>
        public static async Task<IgnoreFile> CreateAsync(
            string baseDirectory,
            string? ignorePath = null
        )
        {
            List<string> patterns = ["**/bin/**", "**/obj/**", "**/.git/**", "**/node_modules/**"];

            string ignoreFilePath;
            if (!string.IsNullOrEmpty(ignorePath))
            {
                ignoreFilePath = ignorePath;
                if (!File.Exists(ignoreFilePath))
                {
                    throw new FileNotFoundException($"Ignore file not found at: {ignoreFilePath}");
                }
            }
            else
            {
                ignoreFilePath = Path.Combine(baseDirectory, ".biceplocalgenignore");
            }

            if (File.Exists(ignoreFilePath))
            {
                string[] lines = await File.ReadAllLinesAsync(ignoreFilePath);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith('#'))
                    {
                        patterns.Add(trimmed);
                    }
                }
            }

            return new IgnoreFile(patterns);
        }

        /// <summary>
        /// Checks if the specified file path should be ignored.
        /// </summary>
        /// <param name="filePath">File path to check.</param>
        /// <returns>True if the file should be ignored.</returns>
        public bool IsIgnored(string filePath)
        {
            string normalizedPath = filePath.Replace('\\', '/');

            foreach (Regex pattern in _compiledPatterns)
            {
                if (pattern.IsMatch(normalizedPath))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<Regex> CompilePatterns(List<string> patterns)
        {
            List<Regex> compiled = [];

            foreach (string pattern in patterns)
            {
                try
                {
                    string regexPattern = ConvertGlobToRegex(pattern);
                    compiled.Add(
                        new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)
                    );
                }
                catch (ArgumentException)
                {
                    // Skip invalid patterns
                    Console.WriteLine($"DEBUG: Skipped invalid pattern '{pattern}'");
                }
            }

            return compiled;
        }

        private static string ConvertGlobToRegex(string glob)
        {
            // Handle simple filenames (no path separators) - they should match anywhere in the path
            if (!glob.Contains('/') && !glob.Contains('\\'))
            {
                string escapedFilename = Regex
                    .Escape(glob)
                    .Replace(@"\*", "[^/]*") // * matches anything except directory separator
                    .Replace(@"\?", "."); // ? matches any single character

                // Match filename at any level: either at root or after any directory separator
                return $@"(^|.*/){escapedFilename}$";
            }

            // Handle paths with directory separators
            string escapedGlob = Regex
                .Escape(glob)
                .Replace(@"\*\*", ".*") // ** matches any number of directories
                .Replace(@"\*", "[^/]*") // * matches anything except directory separator
                .Replace(@"\?", "."); // ? matches any single character

            // For directory patterns (like "Models/**"), allow matching anywhere in the path
            // This handles both relative paths and absolute paths
            if (glob.Contains('/'))
            {
                // Match pattern anywhere in the path (for absolute paths) or at the start (for relative paths)
                return $@"(^{escapedGlob}$|.*/{escapedGlob}$)";
            }

            // For simple patterns without directory separators
            return $"^{escapedGlob}$";
        }
    }
}
