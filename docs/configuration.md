---
title: Configuration
---

`bicep-local-docgen` generates documentation for C# types that have the `[ResourceType]`
attribute from the Bicep package. When this attribute is present, the tool will analyze
the type and generate documentation based on additional Bicep-specific attributes.

## Prerequisites

The documentation tool only processes types that have the `[ResourceType]` attribute
from the `Bicep.Local.Extension.Types.Attributes` namespace. This attribute marks a
class as a Bicep resource type and enables documentation generation,

## Attributes

Configure documentation generation using C# attributes on your types and properties.

### Resource type attributes

#### `[ResourceType]`

Marks a class as a Bicep resource type (from `Bicep.Local.Extension.Types.Attributes` package):

```csharp
using Bicep.Local.Extension.Types.Attributes;

[ResourceType("Demo.Widget")]
public class Widget
{
    // ...
}
```

> [!NOTE]
> Only types with this attribute will be processed by the documentation generator.

#### `[BicepFrontMatter]`

Adds YAML front matter to generated documentation:

```csharp
[BicepFrontMatter("title", "Demo Widget")]
[BicepFrontMatter("description", "A configurable widget resource")]
[BicepFrontMatter("version", "1.0.0", BlockIndex = 2)]
public class Widget
{
    // ...
}
```

#### `[BicepDocHeading]`

Customizes the main heading and description:

```csharp
[BicepDocHeading("Demo Widget", "Manages configurable widget resources")]
public class Widget
{
    // ...
}
```

#### `[BicepDocExample]`

Adds custom examples to the documentation:

```csharp
[BicepDocExample(
    "Basic Widget",
    "Creates a simple widget resource",
    @"resource widget 'Demo.Widget' = {
  name: 'mywidget'
  location: 'East US'
  properties: {
    enabled: true
  }
}",
    "bicep"
)]
public class Widget
{
    // ...
}
```

#### `[BicepDocCustom]`

Adds custom sections to the documentation:

```csharp
[BicepDocCustom("Configuration", "Configure the widget properties before deployment")]
public class Widget
{
    // ...
}
```

### Property attributes

#### `[TypeProperty]`

Configures property documentation (from `Bicep.Local.Extension.Types.Attributes` package):

```csharp
public class Widget
{
    [TypeProperty("The name of the widget", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public string Name { get; set; }

    [TypeProperty("The description of the widget", ObjectTypePropertyFlags.None)]
    public string? Description { get; set; }

    [TypeProperty("The color of the widget", ObjectTypePropertyFlags.None)]
    public Color? Color { get; set; }

    [TypeProperty("The computed size of the widget", ObjectTypePropertyFlags.ReadOnly)]
    public int? Size { get; set; }
}
```

### Property Flags

| Flag         | Description                                     |
|--------------|-------------------------------------------------|
| `Required`   | Property is required in the resource definition |
| `ReadOnly`   | Property is an output attribute only            |
| `Identifier` | Property serves as a unique identifier          |
| `None`       | Property is optional                            |

## Ignore Files

Create a `.biceplocalgenignore` file to exclude files from documentation generation:

```gitignore
# Exclude specific files
Internal/PrivateModels.cs

Models/CustomFolder
```

The ignore file supports:
- File patterns with wildcards (`*`, `?`)
- Directory exclusions (`directory/`)
- Comments (lines starting with `#`)
- Relative paths from the source directory

## MSBuild Integration

Add documentation generation to your build process:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateDocumentation>true</GenerateDocumentation>
    <DocumentationOutputPath>$(MSBuildProjectDirectory)/docs</DocumentationOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="bicep-local-docgen" Version="1.0.0" />
  </ItemGroup>

  <Target Name="GenerateDocumentation" BeforeTargets="Build" Condition="'$(GenerateDocumentation)' == 'true'">
    <Exec Command="bicep-local-docgen generate $(MSBuildProjectDirectory)/Models --output $(DocumentationOutputPath) --force" />
  </Target>

</Project>
```

## Directory structure

The tool expects and generates the following structure:

```plaintext
your-project/
├── src/
│   └── Models/
│       ├── Widget.cs                 # Source files with [ResourceType] attribute
│       └── Button.cs
├── docs/                             # Generated documentation
│   ├── widget.md
│   └── button.md
└── .biceplocalgenignore              # Optional ignore file
```
