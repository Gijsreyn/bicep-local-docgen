
<!-- markdownlint-disable MD041 -->
![Bicep.LocalDeploy.DocGenerator](./banner.svg)

The Bicep.LocalDeploy.DocGenerator project is a documentation tool to generate
Markdown files based on .NET Bicep models. It assists by reading the attributes
from the model you have defined for your Bicep `local-deploy` and generating
reference documentation(examples, arguments, and outputs).

The `bicep-local-docgen` command-line utility (CLI) has two options
available:

- `generate`: Generate documentation files from Bicep models.
- `check`: Check if models have available attributes to leverage in documentation

Each subcommand provides help information. If you call
`bicep-local-docgen generate --help`, it shows you the help
description for the options available.

| Project                              | Description                                                            |
|--------------------------------------|------------------------------------------------------------------------|
| [Bicep.LocalDeploy][00]              | Library for Bicep model annotations                                    |
| [Bicep.LocalDeploy.DocGenerator][01] | CLI utility to generate Markdown files from Bicep models (`*cs`) files |

## Getting started

To install the CLI utility globally, use the following command:

```bash
dotnet tool install Bicep.LocalDeploy.DocGenerator -g
```

Then, use the following command to generate documentation in the `Models`
directory:

```bash
bicep-local-docgen generate --source Models
```

The CLI's default output is the `docs` directory. Use the `--output`
or `--pattern` options to customize the behavior of the CLI. If you
want to see verbose message, add the `--verbose` option to log messages
to the console.

## Using attributes for documentation

This project includes a .NET library that can be used to customize
the rendered Markdown output. In .NET, you use attributes to annotate
your models. Add the `Bicep.LocalDeploy` package to your project to use them.

To add the library to your project, simply run the following command:

```bash
dotnet add package Bicep.LocalDeploy --version <versionNumber>
```

You can annotate your models with these attributes:

- `[BicepDocMetadataAttribute]`: Adds documentation metadata (YAML front matter) as key/value pairs.
- `[BicepMetadataAttribute]`: Adds model metadata as key/value pairs for the generator to consume.
- `[BicepDocExampleAttribute]`: Adds example blocks (title, description, code[, language]).
- `[BicepDocCustomAttribute]`: Adds custom sections (title, description, body) to the output.

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

```markdown
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
```

If you add annotations in the following way:

```csharp
[BicepDocMetadata("title", "My Own Resource")]
[BicepDocMetadata("description", "Example resource used to demonstrate documentation generation.")]
[BicepMetadata("category", "resource")]
[BicepDocExample(
        "Basic deployment",
        "Minimal example creating the resource.",
        @"resource my 'MyOwnResource' = {
Resource: 'my-resource'
}")]

// Rest of the code
```

The produces output is the following Markdown:

```markdown
<!-- myresource.md -->
---
category: "workspace"
title: "My Own Resource"
---

# MyOwnResource

Example resource used to demonstrate documentation generation.

## Example usage

### Basic deployment

Minimal example creating the resource.

    ```bicep
    resource my 'resource my 'MyOwnResource' = {
        Resource: 'my-resource'
    }
    ```

## Notes

Additional guidance

- This section is added via BicepDocCustomAttribute
- Use it for callouts, caveats, or links.
```

<!-- Link reference definitions -->
[00]: https://www.nuget.org/packages/Bicep.LocalDeploy
[01]: https://www.nuget.org/packages/Bicep.LocalDeploy.DocGenerator
