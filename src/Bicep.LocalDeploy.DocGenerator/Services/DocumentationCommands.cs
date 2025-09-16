using System.CommandLine;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Bicep.LocalDeploy.DocGenerator.Services
{
    /// <summary>
    /// Command factory for bicep-local-docgen CLI commands following CSharpier pattern.
    /// </summary>
    internal static class DocumentationCommands
    {
        private static readonly string[] DefaultPatterns = ["*.cs"];

        /// <summary>
        /// Creates the generate command for documentation generation.
        /// </summary>
        /// <returns>The configured generate command.</returns>
        public static Command CreateGenerateCommand()
        {
            Command cmd = new("generate", "Generate documentation files from Bicep models.");

            Argument<string[]> directoryOrFileArgument = new("directoryOrFile")
            {
                Arity = ArgumentArity.ZeroOrMore,
                Description =
                    "One or more paths to directories containing files to process or specific files to process. If omitted, uses current directory.",
            };
            cmd.AddArgument(directoryOrFileArgument);

            Option<DirectoryInfo> outputOption = new(
                ["--output", "-o"],
                "The output directory to store generated docs."
            )
            {
                Arity = ArgumentArity.ExactlyOne,
            };
            outputOption.SetDefaultValue(new DirectoryInfo("docs"));

            Option<string> ignorePathOption = new(
                ["--ignore-path"],
                "Path to the bicep-local-docgen ignore file (.biceplocalgenignore)"
            );

            Option<BicepLogLevel> logLevelOption = new(
                ["--log-level"],
                () => BicepLogLevel.Information,
                "Specify the log level - Critical, Debug, Error, Information (default), None, Trace, Warning"
            );

            Option<bool> verboseOption = new(["--verbose", "-v"], "Enable verbosity logging.");

            Option<bool> forceOption = new(
                ["--force", "-f"],
                "Overwrite existing files if they already exist."
            );

            cmd.Add(outputOption);
            cmd.Add(ignorePathOption);
            cmd.Add(logLevelOption);
            cmd.Add(verboseOption);
            cmd.Add(forceOption);

            cmd.SetHandler(
                async (directoryOrFile, output, ignorePath, logLevel, verbose, force) =>
                {
                    List<DirectoryInfo> sourceDirectories = GetSourceDirectories(directoryOrFile);

                    GenerationOptions options = new()
                    {
                        SourceDirectories = [.. sourceDirectories],
                        FilePatterns = DefaultPatterns,
                        OutputDirectory = output,
                        Verbose = verbose,
                        Force = force,
                        IgnorePath = ignorePath,
                        LogLevel = (LogLevel)logLevel,
                    };

                    await DocumentationGenerator.GenerateAsync(options);
                },
                directoryOrFileArgument,
                outputOption,
                ignorePathOption,
                logLevelOption,
                verboseOption,
                forceOption
            );

            return cmd;
        }

        /// <summary>
        /// Creates the check command for documentation validation.
        /// </summary>
        /// <returns>The configured check command.</returns>
        public static Command CreateCheckCommand()
        {
            Command cmd = new(
                "check",
                "Check if models have the required documentation attributes."
            );

            Argument<string[]> directoryOrFileArgument = new("directoryOrFile")
            {
                Arity = ArgumentArity.ZeroOrMore,
                Description =
                    "One or more paths to directories containing files to check or specific files to check. If omitted, uses current directory.",
            };
            cmd.AddArgument(directoryOrFileArgument);

            Option<string> ignorePathOption = new(
                ["--ignore-path"],
                "Path to the bicep-local-docgen ignore file (.biceplocalgenignore)"
            );

            Option<BicepLogLevel> logLevelOption = new(
                ["--log-level"],
                () => BicepLogLevel.Information,
                "Specify the log level - Critical, Debug, Error, Information (default), None, Trace, Warning"
            );

            Option<bool> includeExtendedOption = new(
                ["--include-extended"],
                "Include validation for BicepFrontMatter and BicepDocCustom attributes"
            );

            Option<bool> verboseOption = new(["--verbose", "-v"], "Enable verbose logging.");

            cmd.Add(ignorePathOption);
            cmd.Add(logLevelOption);
            cmd.Add(includeExtendedOption);
            cmd.Add(verboseOption);

            cmd.SetHandler(
                async (directoryOrFile, ignorePath, logLevel, includeExtended, verbose) =>
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    ConsoleLogger logger = new((LogLevel)logLevel);
                    CheckValidator validator = new(logger);
                    CheckFormatter formatter = new(logger);

                    List<DirectoryInfo> sourceDirectories = GetSourceDirectories(directoryOrFile);

                    CheckOptions options = new()
                    {
                        SourceDirectories = sourceDirectories,
                        FilePatterns = [.. DefaultPatterns],
                        IgnorePath = ignorePath,
                        LogLevel = (LogLevel)logLevel,
                        IncludeExtended = includeExtended,
                        Verbose = verbose,
                    };

                    try
                    {
                        List<CheckResult> results = await validator.ValidateAsync(options);
                        stopwatch.Stop();
                        Environment.ExitCode = formatter.FormatResults(results, stopwatch);
                    }
                    catch (Exception ex)
                    {
                        LogValidationError(logger, ex);
                        Environment.ExitCode = 1;
                    }
                },
                directoryOrFileArgument,
                ignorePathOption,
                logLevelOption,
                includeExtendedOption,
                verboseOption
            );

            return cmd;
        }

        /// <summary>
        /// Converts string paths to DirectoryInfo objects, handling both files and directories.
        /// </summary>
        /// <param name="directoryOrFile">Array of directory or file paths.</param>
        /// <returns>List of DirectoryInfo objects representing source directories.</returns>
        private static List<DirectoryInfo> GetSourceDirectories(string[] directoryOrFile)
        {
            // If no arguments provided, use current directory
            if (directoryOrFile == null || directoryOrFile.Length == 0)
            {
                return [new DirectoryInfo(Directory.GetCurrentDirectory())];
            }

            List<DirectoryInfo> sourceDirectories = [];

            foreach (string path in directoryOrFile)
            {
                if (Directory.Exists(path))
                {
                    sourceDirectories.Add(new DirectoryInfo(path));
                }
                else if (File.Exists(path))
                {
                    // If it's a file, add its containing directory
                    string? directoryPath = Path.GetDirectoryName(Path.GetFullPath(path));
                    if (directoryPath != null)
                    {
                        sourceDirectories.Add(new DirectoryInfo(directoryPath));
                    }
                }
                else
                {
                    throw new DirectoryNotFoundException($"Path not found: {path}");
                }
            }

            return [.. sourceDirectories.Distinct()];
        }

        // LoggerMessage delegate for performance
        private static readonly Action<ILogger, Exception?> LogValidationError =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(6, nameof(LogValidationError)),
                "An error occurred during validation"
            );
    }
}
