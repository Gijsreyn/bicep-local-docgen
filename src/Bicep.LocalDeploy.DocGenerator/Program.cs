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
            RootCommand root = new(
                "Generate documentation files from Bicep models having the [ResourceType] attribute and additional custom attributes from Bicep.LocalDeploy project."
            );

            root.AddCommand(DocumentationCommands.CreateGenerateCommand());
            root.AddCommand(DocumentationCommands.CreateCheckCommand());

            Parser parser = new CommandLineBuilder(root).UseDefaults().UseVersionOption().Build();

            return await parser.InvokeAsync(args);
        }
    }
}
