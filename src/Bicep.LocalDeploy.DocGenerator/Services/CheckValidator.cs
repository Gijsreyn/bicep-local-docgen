using Microsoft.Extensions.Logging;

namespace Bicep.LocalDeploy.DocGenerator.Services
{
    /// <summary>
    /// Options for the check command.
    /// </summary>
    public sealed class CheckOptions
    {
        /// <summary>Source directories to check.</summary>
        public required List<DirectoryInfo> SourceDirectories { get; set; }

        /// <summary>File patterns to include.</summary>
        public required List<string> FilePatterns { get; set; }

        /// <summary>Path to ignore file.</summary>
        public string? IgnorePath { get; set; }

        /// <summary>Log level for output.</summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>Whether to include BicepDocCustom validation.</summary>
        public bool IncludeCustom { get; set; }

        /// <summary>Enable verbose output.</summary>
        public bool Verbose { get; set; }
    }

    /// <summary>
    /// Result of a check operation on a single file.
    /// </summary>
    public sealed class CheckResult
    {
        /// <summary>File path that was checked.</summary>
        public required string FilePath { get; set; }

        /// <summary>True if the file has validation errors.</summary>
        public bool HasErrors { get; set; }

        /// <summary>List of validation errors found.</summary>
        public List<ValidationError> Errors { get; set; } = [];
    }

    /// <summary>
    /// A specific validation error found in a file.
    /// </summary>
    public sealed class ValidationError
    {
        /// <summary>Line number where the error occurred.</summary>
        public int LineNumber { get; set; }

        /// <summary>Type name that has the error.</summary>
        public required string TypeName { get; set; }

        /// <summary>Description of what is missing.</summary>
        public required string Message { get; set; }

        /// <summary>Expected attribute that should be added.</summary>
        public required string ExpectedAttribute { get; set; }
    }

    /// <summary>
    /// Validator that checks for missing Bicep documentation attributes on resource types.
    /// </summary>
    internal sealed class CheckValidator(ILogger logger)
    {
        /// <summary>
        /// Validates all files in the specified options and returns results.
        /// </summary>
        /// <param name="options">Check options specifying what to validate.</param>
        /// <returns>List of check results for each file processed.</returns>
        public async Task<List<CheckResult>> ValidateAsync(CheckOptions options)
        {
            List<CheckResult> results = [];
            IgnoreFile ignoreFile = await IgnoreFile.CreateAsync(
                options.SourceDirectories.First().FullName,
                options.IgnorePath
            );

            // Discover files
            List<FileInfo> files = [];
            foreach (DirectoryInfo dir in options.SourceDirectories)
            {
                if (!dir.Exists)
                {
                    continue;
                }

                foreach (string pattern in options.FilePatterns)
                {
                    files.AddRange(dir.GetFiles(pattern, SearchOption.AllDirectories));
                }
            }

            // Remove duplicates and filter ignored files
            List<FileInfo> uniqueFiles =
            [
                .. files
                    .GroupBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .Where(f => !ignoreFile.IsIgnored(f.FullName)),
            ];

            LogCheckingFiles(logger, uniqueFiles.Count, null);

            foreach (FileInfo file in uniqueFiles)
            {
                if (options.Verbose)
                {
                    LogCheckingFile(logger, file.FullName, null);
                }

                CheckResult result = await ValidateFileAsync(file, options);
                results.Add(result);
            }

            return results;
        }

        private async Task<CheckResult> ValidateFileAsync(FileInfo file, CheckOptions options)
        {
            CheckResult result = new() { FilePath = file.FullName };

            try
            {
                string content = await File.ReadAllTextAsync(file.FullName);
                GenerationOptions analysisOptions = new()
                {
                    SourceDirectories = [file.Directory!],
                    FilePatterns = [file.Name],
                    OutputDirectory = new DirectoryInfo(Path.GetTempPath()),
                    Verbose = false,
                    Force = false,
                };

                AnalysisResult analysis = await RoslynAnalyzer.AnalyzeAsync(analysisOptions);

                // Check each resource type
                foreach (
                    TypeInfoModel type in analysis.Types.Where(t => t.ResourceTypeName is not null)
                )
                {
                    ValidateResourceType(type, result, content.Split('\n'), options);
                }
            }
            catch (Exception ex)
            {
                LogAnalysisError(logger, file.FullName, ex);
            }

            result.HasErrors = result.Errors.Count > 0;
            return result;
        }

        private static void ValidateResourceType(
            TypeInfoModel type,
            CheckResult result,
            string[] lines,
            CheckOptions options
        )
        {
            int typeLineNumber = FindTypeLineNumber(type.Name, lines);

            // Check for BicepDocHeading
            if (string.IsNullOrEmpty(type.HeadingTitle))
            {
                result.Errors.Add(
                    new ValidationError
                    {
                        LineNumber = typeLineNumber,
                        TypeName = type.Name,
                        Message = "Missing BicepDocHeading attribute",
                        ExpectedAttribute = "[BicepDocHeading(\"Title\", \"Description\")]",
                    }
                );
            }

            // Check for examples
            if (type.Examples.Count == 0)
            {
                result.Errors.Add(
                    new ValidationError
                    {
                        LineNumber = typeLineNumber,
                        TypeName = type.Name,
                        Message = "Missing BicepDocExample attribute",
                        ExpectedAttribute =
                            "[BicepDocExample(\"Title\", \"Description\", \"code\")]",
                    }
                );
            }

            // Check for front matter
            if (type.FrontMatterBlocks.Count == 0)
            {
                result.Errors.Add(
                    new ValidationError
                    {
                        LineNumber = typeLineNumber,
                        TypeName = type.Name,
                        Message = "Missing BicepFrontMatter attribute",
                        ExpectedAttribute = "[BicepFrontMatter(\"key\", \"value\")]",
                    }
                );
            }

            // Check for custom sections if requested
            if (options.IncludeCustom && type.CustomSections.Count == 0)
            {
                result.Errors.Add(
                    new ValidationError
                    {
                        LineNumber = typeLineNumber,
                        TypeName = type.Name,
                        Message = "Missing BicepDocCustom attribute",
                        ExpectedAttribute = "[BicepDocCustom(\"Title\", \"Description\")]",
                    }
                );
            }
        }

        private static int FindTypeLineNumber(string typeName, string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Contains($"class {typeName}") || line.Contains($"record {typeName}"))
                {
                    return i + 1; // Line numbers are 1-based
                }
            }
            return 1;
        }

        // LoggerMessage delegates for performance
        private static readonly Action<ILogger, int, Exception?> LogCheckingFiles =
            LoggerMessage.Define<int>(
                LogLevel.Information,
                new EventId(3, nameof(LogCheckingFiles)),
                "Checking {FileCount} files..."
            );

        private static readonly Action<ILogger, string, Exception?> LogCheckingFile =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(4, nameof(LogCheckingFile)),
                "Checking {FileName}"
            );

        private static readonly Action<ILogger, string, Exception?> LogAnalysisError =
            LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(5, nameof(LogAnalysisError)),
                "Error analyzing file {FileName}"
            );
    }
}
