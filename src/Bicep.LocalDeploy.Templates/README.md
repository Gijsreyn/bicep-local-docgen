# Bicep.LocalDeploy.Templates

The `Bicep.LocalDeploy.Templates` package contains .NET project templates for Bicep Local Deploy
project structure bootstrapping.

## Install

Install the templates package:

```bash
dotnet new install Bicep.LocalDeploy.Templates
```

## Available templates

List out the available templates:

```bash
dotnet new list --tag Bicep
```

## Usage

### Create a new extension project

```powershell
# Create a new extension with default name
dotnet new bicep-ld-tpl -n MyAwesomeExtension

# Navigate to the project
cd MyAwesomeExtension

# Build and publish the extension
.\build.ps1
```

### Template parameters

- `--extensionName` or `-en`: The name of the extension (default: MyExtension)
- `--extensionVersion` or `-ev`: The version of the extension (default: 0.0.1)

Example with custom parameters:

```bash
dotnet new bicep-ld-tpl -n MyApi -en "MyApiExtension" -ev "1.0.0"
```

## What's included

The template creates a complete project structure:

```plaintext
MyExtension/
 build.ps1                    # Build and publish script
 global.json                  # .NET SDK version configuration
 GlobalUsings.cs              # Global using directives
 Program.cs                   # Application entry point
 MyExtension.csproj           # Project file
 Models/
    Configuration.cs          # Extension configuration
    SampleResource/
      SampleResource.cs       # Sample resource model
 Handlers/
    ResourceHandlerBase.cs    # Base handler with REST API helpers
    SampleHandler/
      SampleResourceHandler.cs # Sample resource handler
```

## Next steps

After creating your project:

1. **Customize the configuration**: Update `Models/Configuration.cs` with your required settings.
2. **Define your resources**: Modify or add resource models in `Models/` directory.
3. **Implement handlers**: Create handlers in `Handlers/` directory for your resources.
4. **Register handlers**: Add your handlers to `Program.cs`.
5. **Generate documentation**: Run `bicep-local-docgen generate . --output docs`.

## Resources

- [Bicep Local Deploy Documentation](https://github.com/Gijsreyn/bicep-local-docgen)
- [Bicep documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Sample extension](https://github.com/Gijsreyn/bicep-ext-databricks/tree/main)

## Support

For issues, questions, or contributions, visit the [GitHub repository](https://github.com/Gijsreyn/bicep-local-docgen).

## License

This template package is provided under the MIT License.
