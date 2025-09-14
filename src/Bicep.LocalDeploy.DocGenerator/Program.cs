using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Bicep.LocalDeploy.DocGenerator.Services;

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
                "Check if models have available attributes to leverage in documentation."
            );

            // For now, this is a stub implementation.
            cmd.SetHandler(() =>
            {
                Console.WriteLine(
                    "The 'check' command is not implemented yet. Please use 'generate' for now."
                );
            });

            return cmd;
        }
    }
}
