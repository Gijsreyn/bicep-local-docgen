<!-- markdownlint-disable MD041 -->
![Bicep.LocalDeploy.DocGenerator](./banner.svg)

The Bicep.LocalDeploy.DocGenerator project is a documentation tool to generate
Markdown files based on .NET Bicep models. It assists by reading the attributes
from the model files (`*.cs`) you have defined for your Bicep `local-deploy` and
generating reference documentation (examples, arguments, and outputs).

The `bicep-local-docgen` command-line utility (CLI) has two options
available:

- `generate`: Generate documentation files from Bicep models.
- `check`: Check if models have available attributes to leverage in documentation

Each subcommand provides help information. If you call
`bicep-local-docgen generate --help`, it shows you the help
description for the options available.

| Project                           | Description                                                                |
|-----------------------------------|----------------------------------------------------------------------------|
| [Bicep.LocalDeploy][00]           | Library for Bicep model annotations                                        |
| [bicep-local-docgen][01]          | CLI utility to generate Markdown files from Bicep models (`*cs`) files     |
| [Bicep.LocalDeploy.Templates][04] | .NET template package to bootstrap common `local-deploy` project structure |

## Getting started

To install the CLI utility globally, use the following command:

```bash
dotnet tool install bicep-local-docgen -g
```

Then, target a directory containing your Bicep model files. For example,
the following command generates documentation in the `Models` directory:

```bash
bicep-local-docgen generate --source Models
```

The CLI's default output is the `docs` directory. Use the `--output`
or `--pattern` options to customize the behavior of the CLI. If you
want to see verbose message, add the `--verbose` option to log messages
to the console.

[For the full documentation, check it out on GitHub][03].

## Starting from template package

To quickly bootstrap a new Bicep `local-deploy` extension project, install
the `Bicep.LocalDeploy.Templates` package and use the `dotnet new` command:

```bash
dotnet new install Bicep.LocalDeploy.Templates
dotnet new bicep-ld-tpl -n MyExtension
```

This creates a complete project structure with example handlers, models,
and configuration. The template includes the necessary package references
and demonstrates best practices for building Bicep extensions. In the root
of the project, a `build.ps1` file is added, allowing you to easily
build and publish the Bicep extension locally. Simply open a PowerShell 7+
terminal session and run `.\build.ps1`.

> [!NOTE]
> Make sure you change the .NET SDK version available on your system
> after the template structure is created.

## Using attributes for documentation

This project includes a .NET library that can be used to customize
the rendered Markdown output. In .NET, you use attributes to annotate
your models. Add the `Bicep.LocalDeploy` package to your project to use them.

To add the library to your project, simply run the following command:

```bash
dotnet add package Bicep.LocalDeploy
```

You can annotate your models with these attributes:

- `[BicepDocHeadingAttribute]`: Sets the first heading (H1) title and its description.
- `[BicepFrontMatterAttribute]`: Adds YAML front matter key/value pairs.
- `[BicepDocExampleAttribute]`: Adds example blocks (title, description, code[, language]).
- `[BicepDocCustomAttribute]`: Adds custom sections (title, description).

For example, imagine you've defined the following Bicep model:

```csharp
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace MyOwnModel;

[ResourceType("MyOwnResource")]
public class MyOwnResource : DirectoryIdentifiers
{

}

public class MyOwnResourceIdentifiers
{
    [TypeProperty("A demo resource.", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public required string Resource { get; set; }
}
```

When you run the `bicep-local-docgen` tool, the produces output will be:

````markdown
<!-- myresource.md -->
# MyOwnResource

Manages MyOwnResource resources.

## Example usage

### Basic MyOwnResource

Creating a basic MyOwnResource resource:

```bicep
resource myOwnResource 'MyOwnResource' = {
}
```
````

If you add annotations in the following way:

```csharp
[BicepDocHeading("My Own Resource", "Example resource used to demonstrate documentation generation.")]
[BicepFrontMatter("category", "resource")]
[BicepDocExample(
        "Basic deployment",
        "Minimal example creating the resource.",
        @"resource my 'MyOwnResource' = {
Resource: 'my-resource'
}")]
[BicepDocCustom("Custom Section", "This is a custom section.")]
// Rest of the code
```

The produces output is the following Markdown:

````markdown
<!-- myresource.md -->
---
category: "resource"
---

# My Own Resource

Example resource used to demonstrate documentation generation.

## Example usage

### Basic deployment

Minimal example creating the resource.

```bicep
resource my 'MyOwnResource' = {
Resource: 'my-resource'
}
```

## Custom Section

This is a custom section.
````

## Contributing

Want to contribute to the project? We'd love to have you! Visit our [CONTRIBUTING.md][02]
for a jump start.

<!-- Link reference definitions -->
[00]: https://www.nuget.org/packages/Bicep.LocalDeploy
[01]: https://www.nuget.org/packages/bicep-local-docgen
[02]: CONTRIBUTING.md
[03]: https://github.com/Gijsreyn/bicep-local-docgen/blob/main/docs/README.md
[04]: https://www.nuget.org/packages/Bicep.LocalDeploy.Templates
