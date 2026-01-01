# MyExtension

A Bicep Local Deploy extension template that demonstrates best practices for creating custom Bicep extensions.

Make it your own.

## Overview

This extension provides a sample implementation of a Bicep Local Deploy extension, including:

- **Configuration**: Extension configuration with required base URL.
- **Resource handler**: CRUD operations for sample resources.
- **REST API integration**: HTTP client setup with authentication sample.
- **Documentation attributes**: Full usage of Bicep documentation attributes from Bicep.LocalDeploy.DocGenerator.
- **Property flags**: Demonstration of all `ObjectTypePropertyFlags`.

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

**Required properties:**

- `name` (string, identifier): The unique name of the resource

**Optional properties:**

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

### Example usage

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

## Project structure

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
 Tests/                       # Unit test project
    MyExtension.Tests.csproj  # Test project file
    GlobalUsings.cs           # Global test usings
    README.md                 # Testing documentation
    Handlers/
      MyResourceHandlerTests.cs # Sample handler tests
```

## Customization

### Adding new resources

Follow these steps to add a new resource to your extension:

#### 1. Create the resource model

Create a new folder under `Models/` for your resource (e.g., `Models/MyResource/`):

```csharp
using Bicep.Local.Extension.Types.Attributes;

namespace MyExtension.Models.MyResource;

[BicepDocHeading("MyResource", "Manages MyResource resources.")]
[BicepDocExample(
    "Basic MyResource",
    "Creating a basic MyResource resource.",
    @"resource myRes 'MyResource' = {
  name: 'my-resource'
  description: 'Example resource'
}")]
[ResourceType("MyResource")]
public class MyResource : MyResourceIdentifiers
{
    [TypeProperty("Description of the resource.")]
    public string? Description { get; set; }

    [TypeProperty("The resource ID.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ResourceId { get; set; }
}

public class MyResourceIdentifiers
{
    [TypeProperty("The unique name.", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public required string Name { get; set; }
}
```

#### 2. Create the resource handler

Create a new folder under `Handlers/` for your handler (e.g., `Handlers/MyHandler/`):

```csharp
using Azure.Bicep.Local.Extension;
using MyExtension.Handlers;
using MyExtension.Models;
using MyExtension.Models.MyResource;

namespace MyExtension.Handlers.MyHandler;

public class MyResourceHandler(IHttpClientFactory httpClientFactory, ILogger<MyResourceHandler> logger)
    : ResourceHandlerBase<MyResource, MyResourceIdentifiers, Configuration>(httpClientFactory, logger)
{
    public override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var properties = request.GetResource<MyResource>();
        var identifiers = GetIdentifiers(properties);
        
        // Check if resource exists
        var existing = await GetResourceAsync(identifiers.Name, cancellationToken);
        
        if (existing != null)
        {
            properties.ResourceId = existing.ResourceId;
        }
        
        return GetResponse(request);
    }

    public override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var properties = request.GetResource<MyResource>();
        var identifiers = GetIdentifiers(properties);
        
        var existing = await GetResourceAsync(identifiers.Name, cancellationToken);
        
        if (existing == null)
        {
            await CreateResourceAsync(properties, cancellationToken);
        }
        else
        {
            await UpdateResourceAsync(properties, cancellationToken);
        }
        
        return GetResponse(request);
    }

    public override MyResourceIdentifiers GetIdentifiers(MyResource properties)
    {
        return new MyResourceIdentifiers { Name = properties.Name };
    }

    private async Task<MyResource?> GetResourceAsync(string name, CancellationToken cancellationToken)
    {
        // Implement GET logic
        return await CallApiForResponse<MyResource>(HttpMethod.Get, $"/api/resources/{name}", cancellationToken);
    }

    private async Task CreateResourceAsync(MyResource resource, CancellationToken cancellationToken)
    {
        // Implement CREATE logic
        await CallApi(HttpMethod.Post, "/api/resources", resource, cancellationToken);
    }

    private async Task UpdateResourceAsync(MyResource resource, CancellationToken cancellationToken)
    {
        // Implement UPDATE logic
        await CallApi(HttpMethod.Put, $"/api/resources/{resource.Name}", resource, cancellationToken);
    }
}
```

#### 3. Register the handler

Add the handler registration in `Program.cs`:

```csharp
builder.Services
    .AddBicepExtensionHost()
    .AddBicepExtension<Configuration>()
    .WithResourceHandler<SampleResourceHandler>()
    .WithResourceHandler<MyResourceHandler>(); // Add your new handler
```

#### 4. Update namespaces (if needed)

If you changed the extension name during template creation, ensure all
namespaces are consistent across your files. Use Find and Replace to update
`MyExtension` to your actual extension name throughout the project.

#### 5. Build and test

Build the extension and verify there are no compilation errors:

```powershell
.\build.ps1
```

#### 6. Generate documentation

Generate documentation for your new resource:

```bash
bicep-local-docgen generate . --output docs
```

#### 7. Write tests

The template includes a comprehensive test project under `Tests/` that follows best practices from the [Bicep Local Deploy Unit Testing Guide](https://github.com/Azure/bicep/blob/main/docs/experimental/local-deploy-dotnet-unittesting-guide.md).

**Test project structure:**
- Uses MSTest for test framework
- Moq for mocking dependencies
- FluentAssertions for readable assertions
- Includes sample handler tests demonstrating common patterns

**Running tests:**

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~MyResourceHandlerTests"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Adding tests for your handler:**

Create a new test file in `Tests/Handlers/` for your handler:

```csharp
namespace MyExtension.Tests.Handlers;

[TestClass]
public class MyResourceHandlerTests
{
    private Mock<IHttpClientFactory>? _mockFactory;
    private Mock<ILogger<MyResourceHandler>>? _mockLogger;
    private MyResourceHandler? _handler;

    [TestInitialize]
    public void Setup()
    {
        _mockFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<MyResourceHandler>>();
        _handler = new MyResourceHandler(_mockFactory.Object, _mockLogger.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mockFactory?.VerifyAll();
    }

    [TestMethod]
    public async Task CreateOrUpdate_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        var properties = new MyResource { Name = "test" };
        
        // Act
        var result = await _handler!.CreateOrUpdate(properties, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
    }
}
```

See `Tests/README.md` for detailed testing documentation and patterns.

### Documentation attributes

Use these attributes to generate comprehensive documentation:

- `[BicepFrontMatter]`: Add front matter metadata.
- `[BicepDocHeading]`: Set heading and description.
- `[BicepDocExample]`: Provide usage examples.
- `[BicepDocCustom]`: Add custom documentation sections.

For more information, check out the documentation on [GitHub](https://github.com/Gijsreyn/bicep-local-docgen/blob/main/docs/configuration.md).

## Development

### Build and publish extension

Run the build script to compile and publish your extension:

```powershell
.\build.ps1
```

The build script will:
1. Clean previous builds (if `-Clean` flag is used)
2. Build the extension project
3. Run unit tests (if Tests project exists)
4. Publish the extension for multiple platforms (win-x64, linux-x64, osx-x64)
5. Package the extension using Bicep CLI

### Running tests

The template includes a comprehensive test suite. Tests are automatically run during the Release build:

```powershell
# Build and run tests
.\build.ps1

# Run tests only (from Tests directory)
cd Tests
dotnet test

# Run with code coverage
cd Tests
dotnet test --collect:"XPlat Code Coverage"
```

For more information on testing, see [Tests/README.md](Tests/README.md).

## Generating documentation

Use the Bicep.LocalDeploy.DocGenerator tool to generate documentation:

```bash
dotnet tool install -g Bicep.LocalDeploy.DocGenerator
bicep-local-docgen generate . --output docs
```

## Testing locally

To test the extension locally, add a `bicepconfig.json` file in the root:

```json
{
  "experimentalFeaturesEnabled": {
    "localDeploy": true,
    "extensibility": true
  },
  "extensions": {
    "MyExtension": "output/MyExtension"
  },
  "implicitExtensions": []
}
```
