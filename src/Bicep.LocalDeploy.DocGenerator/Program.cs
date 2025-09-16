using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Bicep.LocalDeploy.DocGenerator.Services;
using Microsoft.Extensions.Logging;

namespace Bicep.LocalDeploy.DocGenerator
{
    internal static class Program
    {
        private static readonly string[] DefaultPatterns = ["*.cs"]; // CA1861

        public static async Task<int> Main(string[] args)
        {
            RootCommand root = new(
                "Generate documentation files from Bicep models having the [ResourceType] attribute and additional custom attributes from Bicep.LocalDeploy project."
            );

            Command generateCmd = CreateGenerateCommand();
            root.Add(generateCmd);

            // TODO: 'check' command: wired but not implemented yet
            Command checkCmd = CreateCheckCommand();
            root.Add(checkCmd);

            Parser parser = new CommandLineBuilder(root).UseDefaults().UseVersionOption().Build();

            return await parser.InvokeAsync(args);
        }

        private static Command CreateGenerateCommand()
        {
            Command cmd = new("generate", "Generate documentation files from Bicep models.");

            Option<DirectoryInfo> sourceOption = new(
                ["--source", "-s"],
                "The source folder storing Bicep models."
            )
            {
                Arity = ArgumentArity.ExactlyOne,
            };

            Option<DirectoryInfo> outputOption = new(
                ["--output", "-o"],
                "The output directory to store generated docs."
            )
            {
                Arity = ArgumentArity.ExactlyOne,
            };
            outputOption.SetDefaultValue(new DirectoryInfo("docs"));

            Option<string[]> patternOption = new(
                ["--pattern", "-p"],
                "Filters to select sources files e.g. *.cs"
            )
            {
                Arity = ArgumentArity.ZeroOrMore,
            };
            patternOption.SetDefaultValue(DefaultPatterns);

            Option<bool> verboseOption = new(["--verbose", "-v"], "Enable verbosity logging.");
            Option<bool> forceOption = new(
                ["--force", "-f"],
                "Overwrite existing files if they already exist."
            );

            cmd.Add(sourceOption);
            cmd.Add(outputOption);
            cmd.Add(patternOption);
            cmd.Add(verboseOption);
            cmd.Add(forceOption);

            cmd.SetHandler(
                (source, output, patterns, verbose, force) =>
                {
                    GenerationOptions options = new()
                    {
                        SourceDirectories = [source],
                        FilePatterns = patterns,
                        OutputDirectory = output,
                        Verbose = verbose,
                        Force = force,
                    };

                    return DocumentationGenerator.GenerateAsync(options);
                },
                sourceOption,
                outputOption,
                patternOption,
                verboseOption,
                forceOption
            );

            return cmd;
        }

        private static Command CreateCheckCommand()
        {
            Command cmd = new(
                "check",
                "Check if models have the required documentation attributes."
            );

            Option<DirectoryInfo[]> sourceOption = new(
                ["--source", "-s"],
                "The source folder(s) storing Bicep models."
            )
            {
                Arity = ArgumentArity.OneOrMore,
            };

            Option<string[]> patternOption = new(
                ["--pattern", "-p"],
                "Filters to select source files e.g. *.cs"
            )
            {
                Arity = ArgumentArity.ZeroOrMore,
            };
            patternOption.SetDefaultValue(DefaultPatterns);

            Option<string> ignorePathOption = new(
                ["--ignore-path"],
                "Path to the bicep-local-docgen ignore file (.biceplocalgenignore)"
            );

            Option<BicepLogLevel> logLevelOption = new(
                ["--log-level"],
                () => BicepLogLevel.Information,
                "Specify the log level - Critical, Debug, Error, Information (default), None, Trace, Warning"
            );

            Option<bool> includeCustomOption = new(
                ["--include-custom"],
                "Include validation for BicepDocCustom attributes"
            );

            Option<bool> verboseOption = new(["--verbose", "-v"], "Enable verbose logging.");

            cmd.Add(sourceOption);
            cmd.Add(patternOption);
            cmd.Add(ignorePathOption);
            cmd.Add(logLevelOption);
            cmd.Add(includeCustomOption);
            cmd.Add(verboseOption);

            cmd.SetHandler(
                async (sources, patterns, ignorePath, logLevel, includeCustom, verbose) =>
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    ConsoleLogger logger = new((LogLevel)logLevel);
                    CheckValidator validator = new(logger);
                    CheckFormatter formatter = new(logger);

                    CheckOptions options = new()
                    {
                        SourceDirectories = [.. sources],
                        FilePatterns = [.. patterns],
                        IgnorePath = ignorePath,
                        LogLevel = (LogLevel)logLevel,
                        IncludeCustom = includeCustom,
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
                sourceOption,
                patternOption,
                ignorePathOption,
                logLevelOption,
                includeCustomOption,
                verboseOption
            );

            return cmd;
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
