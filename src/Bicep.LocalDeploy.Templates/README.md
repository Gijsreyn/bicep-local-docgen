# Bicep.LocalDeploy.Templates

Official project templates for creating Bicep Local Deploy extensions.

## Installation

Install the templates package:

```bash
dotnet new install Bicep.LocalDeploy.Templates
```

## Available Templates

### Bicep Local Deploy Extension (`bicep-extension`)

Creates a complete Bicep Local Deploy extension project with:

-  Extension configuration with required properties
-  Sample resource handler with full CRUD operations
-  REST API integration with authentication
-  Comprehensive documentation attributes
-  All `ObjectTypePropertyFlags` demonstrated
-  Base handler class for reusability
-  Sample models with nested objects and enums
-  Ready-to-use project structure

## Usage

### Create a new extension project

```bash
# Create a new extension with default name
dotnet new bicep-extension -n MyAwesomeExtension

# Navigate to the project
cd MyAwesomeExtension

# Build the project
dotnet build

# Run the extension
dotnet run
```

### Template Parameters

- `--extensionName` or `-en`: The name of the extension (default: MyExtension)
- `--extensionVersion` or `-ev`: The version of the extension (default: 0.0.1)

Example with custom parameters:

```bash
dotnet new bicep-extension -n MyApi -en "MyApiExtension" -ev "1.0.0"
```

## What's Included

The template creates a complete project structure:

```
MyExtension/
 .biceplocalgenignore          # Configure doc generation exclusions
 .gitignore                    # Git ignore file
 GlobalUsings.cs               # Global using directives for DocGenerator
 Program.cs                    # Entry point with resource handler registration
 MyExtension.csproj           # Project file with necessary packages
 README.md                     # Project documentation
 Models/
    Configuration.cs         # Extension configuration class
    SampleResource.cs        # Comprehensive sample resource model
 Handlers/
     ResourceHandlerBase.cs   # Base handler with REST API helpers
     SampleResourceHandler.cs # Sample resource CRUD implementation
```

### Sample Resource Features

The `SampleResource` model demonstrates:

- **All Property Flags**:
  - `Required`: Must be specified by user
  - `Identifier`: Used to identify the resource
  - `ReadOnly`: Output-only properties
  - Combined flags for complex scenarios

- **All Documentation Attributes**:
  - `[BicepFrontMatter]`: Front matter metadata
  - `[BicepDocHeading]`: Heading and description
  - `[BicepDocExample]`: Multiple usage examples
  - `[BicepDocCustom]`: Custom documentation sections

- **Property Types**:
  - Required string identifiers
  - Optional nullable properties
  - Boolean properties with defaults
  - Enum properties with JSON serialization
  - Numeric properties
  - Dictionary properties
  - Nested object properties
  - Read-only output properties

### Handler Implementation

The `SampleResourceHandler` provides:

- Complete CRUD operations (Create, Read, Update, Delete)
- REST API integration with proper error handling
- Logging throughout the lifecycle
- JSON serialization/deserialization
- URL encoding for identifiers
- Response wrapping and validation

### Base Handler Features

The `ResourceHandlerBase<TProps, TIdentifiers>` provides:

- HTTP client factory with configuration
- Authentication via environment variable
- Generic REST API methods
- Request/response logging
- Error handling and reporting
- Support for GET, POST, PUT, PATCH, DELETE methods

## Next Steps

After creating your project:

1. **Customize the Configuration**: Update `Models/Configuration.cs` with your required settings
2. **Define Your Resources**: Modify or add resource models in `Models/` directory
3. **Implement Handlers**: Create handlers in `Handlers/` directory for your resources
4. **Register Handlers**: Add your handlers to `Program.cs`
5. **Generate Documentation**: Run `bicep-local-docgen generate . --output docs`
6. **Test Your Extension**: Create Bicep files and test with `bicep deploy`

## Documentation Generation

Generate comprehensive documentation for your resources:

```bash
# Install the doc generator tool
dotnet tool install -g Bicep.LocalDeploy.DocGenerator

# Generate documentation
bicep-local-docgen generate . --output docs
```

The tool will:
- Parse all your resource models
- Extract documentation from attributes
- Generate Markdown files for each resource
- Include examples, property descriptions, and custom sections

## Example Bicep Usage

After creating your extension, use it in Bicep:

```bicep
targetScope = 'local'

param baseUrl string

extension myExtension with {
  baseUrl: baseUrl
}

resource sample 'SampleResource' = {
  name: 'my-resource'
  description: 'A sample resource'
  isEnabled: true
  status: 'Active'
  tags: {
    environment: 'production'
  }
}

output resourceId string = sample.resourceId
output createdAt string = sample.createdAt
```

## Resources

- [Bicep Local Deploy Documentation](https://github.com/Gijsreyn/bicep-local-docgen)
- [Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Sample Extensions](https://github.com/Gijsreyn/bicep-local-docgen/tree/main/Databricks.Extension)

## Support

For issues, questions, or contributions, visit the [GitHub repository](https://github.com/Gijsreyn/bicep-local-docgen).

## License

This template package is provided under the MIT License.
