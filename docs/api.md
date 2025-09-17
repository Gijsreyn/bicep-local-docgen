---
title: API usage
---

Bicep.LocalDeploy library can be used programmatically to generate documentation from your
C# code.

This requires adding the [Bicep.LocalDeploy][00] NuGet package to your project:

```bash
dotnet add package Bicep.LocalDeploy
```

## Generating Documentation

### Basic Usage

```csharp
using Bicep.LocalDeploy.DocGenerator.Services;

var options = new GenerationOptions
{
    SourceDirectories = [new DirectoryInfo("src/Models")],
    FilePatterns = ["*.cs"],
    OutputDirectory = new DirectoryInfo("docs"),
    Verbose = false,
    Force = true
};

await DocumentationGenerator.GenerateAsync(options);
```

### Advanced Configuration

```csharp
var options = new GenerationOptions
{
    SourceDirectories = [
        new DirectoryInfo("src/Models"),
        new DirectoryInfo("src/Resources")
    ],
    FilePatterns = ["*.cs", "*.Generated.cs"],
    OutputDirectory = new DirectoryInfo("documentation"),
    IgnorePath = ".customignore",
    Verbose = true,
    Force = false
};

await DocumentationGenerator.GenerateAsync(options);
```

## Analysis API

For more advanced scenarios, you can use the analysis components directly:

### Analyzing source code

```csharp
using Bicep.LocalDeploy.DocGenerator.Services;

var options = new GenerationOptions
{
    SourceDirectories = [new DirectoryInfo("src")],
    FilePatterns = ["*.cs"],
    Verbose = true
};

AnalysisResult result = await RoslynAnalyzer.AnalyzeAsync(options);

// Access analyzed types
foreach (TypeInfoModel type in result.Types)
{
    Console.WriteLine($"Found type: {type.Name}");
    Console.WriteLine($"Resource type: {type.ResourceTypeName}");
    Console.WriteLine($"Members: {type.Members.Count}");
}
```

### Using individual components

The refactored architecture provides focused components for specific tasks:

```csharp
using Bicep.LocalDeploy.DocGenerator.Services.Analysis;
using Bicep.LocalDeploy.DocGenerator.Services.Generation;
using Bicep.LocalDeploy.DocGenerator.Services.Formatting;

// Extract attributes from syntax
var frontMatter = AttributeAnalyzer.ExtractFrontMatterBlocks(typeDeclaration);
var examples = AttributeAnalyzer.ExtractExamples(typeDeclaration);

// Generate markdown sections
var sb = new StringBuilder();
FrontMatterGenerator.Generate(sb, frontMatter);
ExampleGenerator.GenerateCustomExamples(sb, examples);

// Validate markdown
string validatedMarkdown = MarkdownValidator.ValidateAndProcess(sb.ToString());
```

## Generation options

| Property            | Type              | Description                          | Default                |
|---------------------|-------------------|--------------------------------------|------------------------|
| `SourceDirectories` | `DirectoryInfo[]` | Directories to scan for source files | Current directory      |
| `FilePatterns`      | `string[]`        | File patterns to include             | `["*.cs"]`             |
| `OutputDirectory`   | `DirectoryInfo`   | Output directory for documentation   | `docs`                 |
| `IgnorePath`        | `string?`         | Path to ignore file                  | `.biceplocalgenignore` |
| `Verbose`           | `bool`            | Enable verbose logging               | `false`                |
| `Force`             | `bool`            | Overwrite existing files             | `false`                |

## Error handling

The API provides detailed error information:

```csharp
try
{
    await DocumentationGenerator.GenerateAsync(options);
}
catch (DirectoryNotFoundException ex)
{
    Console.WriteLine($"Source directory not found: {ex.Message}");
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"Permission denied: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Generation failed: {ex.Message}");
}
```

<!-- Link reference definitions -->
[00]: https://www.nuget.org/packages/Bicep.LocalDeploy
