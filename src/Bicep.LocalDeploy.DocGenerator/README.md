# Bicep.LocalDeploy.DocGenerator

CLI tool that scans C# source for ResourceType and TypeProperty attributes and
generates Terraform-style Markdown docs for Bicep Local Deploy resources.

- Uses System.CommandLine for CLI UX
- Uses Microsoft.CodeAnalysis.CSharp (Roslyn) to parse attributes reliably
- Uses Markdig to build Markdown strings

## Usage

- Build and run from repo root:

```powershell
# Build
dotnet build .\src\Bicep.LocalDeploy.DocGenerator\Bicep.LocalDeploy.DocGenerator.csproj

# Generate docs from your Databricks models folder
bicep-local-docgen generate -s .\src\Databricks.Extension\Models -o .\docs-out
```

## Rules

- TypeProperty flags:
    - Required -> Argument reference (required)
    - ReadOnly -> Attribute reference (output)

- Multiple examples are generated: basic (required only) and advanced (required
  and optional).
