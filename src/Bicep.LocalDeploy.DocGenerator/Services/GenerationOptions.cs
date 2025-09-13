namespace Bicep.LocalDeploy.DocGenerator.Services;

public sealed class GenerationOptions
{
    public DirectoryInfo[] SourceDirectories { get; init; } = Array.Empty<DirectoryInfo>();
    public string[] FilePatterns { get; init; } = ["*.cs"];
    public DirectoryInfo OutputDirectory { get; init; } = new("./docs");
    public bool Verbose { get; init; }
    public bool Force { get; init; }
}
