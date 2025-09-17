---
title: What is bicep-local-docgen?
---

The `bicep-local-docgen` is a documentation CLI generator for Bicep local deployment models.
It analyzes C# source code with Bicep-specific attributes and generates rich
Markdown documentation for resource types.

The tool uses Roslyn for syntax analysis and Markdig for Markdown processing,
providing automated documentation generation that stays in sync with your code.

### Getting started

Generate documentation from your Bicep models with the following command:

```bash
bicep-local-docgen generate src/Models --output docs
```

Then view the generated Markdown files in your `docs/` directory.

See [CLI Usage][01] and [Configuration][02] for more information.

`bicep-local-docgen` can also be used [programmatically][03] and integrated into
[build processes][04] or [CI/CD pipelines][05].

---

### Before

```csharp
[ResourceType("Demo.Widget")]
public class Widget
{
    [TypeProperty("The name of the widget", ObjectTypePropertyFlags.Required)]
    public string Name { get; set; }
}
```

### After

Generated Markdown documentation:

````markdown
---
title: "Demo Widget"
description: "A demo resource used for tests"
---

# Widget

Manages Demo.Widget resources.

## Example usage

### Basic Demo.Widget

Creating a basic Demo.Widget resource:

```bicep
resource widget 'Demo.Widget' = {
  name: 'example'
}
```

## Argument reference

The following arguments are available:

- `name` - (Required) The name of the widget
````

<!-- Link reference definitions -->
[01]: cli.md
[02]: configuration.md
[03]: api.md
[04]: msbuild.md
[05]: continuous-integration.md
