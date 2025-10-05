# MyExtension

A Bicep Local Deploy extension template that demonstrates best practices for creating custom Bicep extensions.

## Overview

This extension provides a sample implementation of a Bicep Local Deploy extension, including:

- **Configuration**: Extension configuration with required base URL
- **Resource Handler**: Complete CRUD operations for sample resources
- **REST API Integration**: HTTP client setup with authentication
- **Documentation Attributes**: Full usage of Bicep documentation attributes
- **Property Flags**: Demonstration of all `ObjectTypePropertyFlags`

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Bicep CLI with local extension support

### Configuration

The extension requires a `baseUrl` parameter for the API endpoint:

```bicep
extension myExtension with {
  baseUrl: 'https://api.example.com'
}
```

### Authentication

Set the `API_KEY` environment variable to authenticate API requests:

```bash
export API_KEY=your-api-key-here
```

## Resources

### SampleResource

A sample resource demonstrating all available features:

**Required Properties:**
- `name` (string, identifier): The unique name of the resource

**Optional Properties:**
- `description` (string): A brief description
- `isEnabled` (bool): Whether the resource is enabled (default: true)
- `status` (enum): The current status (Active, Inactive, Pending, Deleted)
- `maxRetries` (int): Maximum number of retry attempts
- `timeoutSeconds` (int): Timeout in seconds for operations
- `tags` (object): Key-value pairs for resource tags
- `metadata` (object): Additional metadata for the resource

**Output Properties (Read-Only):**
- `resourceId` (string): The unique identifier
- `createdAt` (datetime): Creation timestamp
- `updatedAt` (datetime): Last update timestamp

### Example Usage

```bicep
resource sample 'SampleResource' = {
  name: 'my-sample-resource'
  description: 'A sample resource for demonstration'
  isEnabled: true
  status: 'Active'
  tags: {
    environment: 'development'
    owner: 'team-alpha'
  }
}

output resourceId string = sample.resourceId
output createdAt string = sample.createdAt
```

## Project Structure

```
MyExtension/
 GlobalUsings.cs              # Global using directives
 Program.cs                   # Application entry point
 MyExtension.csproj          # Project file
 Models/
    Configuration.cs        # Extension configuration
    SampleResource.cs       # Sample resource model
 Handlers/
     ResourceHandlerBase.cs  # Base handler with REST API helpers
     SampleResourceHandler.cs # Sample resource handler
```

## Customization

### Adding New Resources

1. Create a new model class in `Models/` directory
2. Decorate with `[ResourceType]` and documentation attributes
3. Create a handler class in `Handlers/` directory inheriting from `ResourceHandlerBase`
4. Register the handler in `Program.cs` using `.WithResourceHandler<YourHandler>()`

### Documentation Attributes

Use these attributes to generate comprehensive documentation:

- `[BicepFrontMatter]`: Add front matter metadata
- `[BicepDocHeading]`: Set heading and description
- `[BicepDocExample]`: Provide usage examples
- `[BicepDocCustom]`: Add custom documentation sections

### Property Flags

Use `ObjectTypePropertyFlags` to control property behavior:

- `Required`: Property must be specified
- `Identifier`: Used to identify the resource
- `ReadOnly`: Cannot be set by user (output only)

## Development

### Building

```bash
dotnet build
```

### Running

```bash
dotnet run
```

### Testing with Bicep

Create a `main.bicep` file and use the extension:

```bicep
targetScope = 'local'

param baseUrl string

extension myExtension with {
  baseUrl: baseUrl
}

resource sample 'SampleResource' = {
  name: 'test-resource'
  description: 'Test resource'
  isEnabled: true
}
```

Create a `main.bicepparam` file:

```bicep
using 'main.bicep'
param baseUrl = 'https://api.example.com'
```

Deploy:

```bash
bicep deploy --file main.bicep --parameters main.bicepparam
```

## Generating Documentation

Use the Bicep.LocalDeploy.DocGenerator tool to generate documentation:

```bash
dotnet tool install -g Bicep.LocalDeploy.DocGenerator
bicep-local-docgen generate . --output docs
```

## License

This template is provided as-is for creating Bicep Local Deploy extensions.
