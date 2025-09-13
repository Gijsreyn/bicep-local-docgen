using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Bicep.LocalDeploy.DocGenerator.Services;

namespace Bicep.LocalDeploy.DocGenerator
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var root = new RootCommand(
                "Generate documentation files from Bicep models having the [ResourceType] attribute and additional custom attributes from Bicep.LocalDeploy project."
            );

            var generateCmd = CreateGenerateCommand();
            root.Add(generateCmd);

            // TODO: 'check' command: wired but not implemented yet
            var checkCmd = CreateCheckCommand();
            root.Add(checkCmd);

            var parser = new CommandLineBuilder(root).UseDefaults().UseVersionOption().Build();

            return await parser.InvokeAsync(args);
        }

        private static Command CreateGenerateCommand()
        {
            var cmd = new Command("generate", "Generate documentation files from Bicep models.");

            var sourceOption = new Option<DirectoryInfo>(
                ["--source", "-s"],
                "The source folder storing Bicep models."
            )
            {
                Arity = ArgumentArity.ExactlyOne,
            };

            var outputOption = new Option<DirectoryInfo>(
                ["--output", "-o"],
                "The output directory to store generated docs."
            )
            {
                Arity = ArgumentArity.ExactlyOne,
            };
            outputOption.SetDefaultValue(new DirectoryInfo("docs"));

            var patternOption = new Option<string[]>(
                ["--pattern", "-p"],
                "Filters to select sources files e.g. *.cs"
            )
            {
                Arity = ArgumentArity.ZeroOrMore,
            };
            patternOption.SetDefaultValue(new[] { "*.cs" });

            var verboseOption = new Option<bool>(["--verbose", "-v"], "Enable verbosity logging.");
            var forceOption = new Option<bool>(
                ["--force", "-f"],
                "Overwrite existing files if they already exist."
            );

            cmd.Add(sourceOption);
            cmd.Add(outputOption);
            cmd.Add(patternOption);
            cmd.Add(verboseOption);
            cmd.Add(forceOption);

            cmd.SetHandler(
                async (
                    DirectoryInfo source,
                    DirectoryInfo output,
                    string[] patterns,
                    bool verbose,
                    bool force
                ) =>
                {
                    GenerationOptions options = new()
                    {
                        SourceDirectories = [source],
                        FilePatterns = patterns,
                        OutputDirectory = output,
                        Verbose = verbose,
                        Force = force,
                    };

                    DocumentationGenerator generator = new();
                    await generator.GenerateAsync(options);
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
            var cmd = new Command("check", "Check if models have available attributes to leverage in documentation.");

            // For now, this is a stub implementation.
            cmd.SetHandler(() =>
            {
                Console.WriteLine("The 'check' command is not implemented yet. Please use 'generate' for now.");
            });

            return cmd;
        }
    }
}
