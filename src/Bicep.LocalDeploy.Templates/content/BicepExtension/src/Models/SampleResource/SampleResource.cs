using System.Text.Json.Serialization;
using Bicep.Local.Extension.Types.Attributes;

namespace MyExtension.Models.SampleResource;

public enum ResourceStatus
{
    Active,
    Inactive,
    Pending,
    Deleted
}

[BicepFrontMatter("category", "Sample")]
[BicepDocHeading("SampleResource", "Represents a sample resource that demonstrates all available documentation attributes.")]
[BicepDocExample(
    "Creating a basic sample resource",
    "This example shows how to create a simple sample resource with required properties.",
    @"resource sample 'SampleResource' = {
  name: 'my-sample-resource'
  description: 'A sample resource for demonstration'
  isEnabled: true
  tags: {
    environment: 'development'
    owner: 'team-alpha'
  }
}
"
)]
[BicepDocExample(
    "Creating an advanced sample resource",
    "This example demonstrates advanced features including nested properties and enums.",
    @"resource advancedSample 'SampleResource' = {
  name: 'advanced-sample'
  description: 'An advanced sample with all features'
  isEnabled: true
  status: 'Active'
  maxRetries: 3
  timeoutSeconds: 30
  tags: {
    environment: 'production'
    criticality: 'high'
  }
  metadata: {
    createdBy: 'automation'
    version: '1.0.0'
  }
}
"
)]
[BicepDocCustom("Notes", @"When working with the `SampleResource` resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
param baseUrl string
extension myExtension with {
  baseUrl: baseUrl
}

resource sample 'SampleResource' = {
  name: 'my-resource'
  description: 'Sample resource'
  isEnabled: true
}

// main.bicepparam
using 'main.bicep'
param baseUrl = 'https://api.example.com'
```")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Sample Resource API Documentation][00]
- [Best Practices Guide][01]

<!-- Link reference definitions -->
[00]: https://docs.example.com/api/sample-resource
[01]: https://docs.example.com/guides/best-practices")]
[ResourceType("SampleResource")]
public class SampleResource : SampleResourceIdentifiers
{
    // Optional writable property
    [TypeProperty("A brief description of the resource.")]
    public string? Description { get; set; }

    // Optional writable property with default
    [TypeProperty("Whether the resource is enabled.")]
    public bool IsEnabled { get; set; } = true;

    // Optional writable enum property
    [TypeProperty("The current status of the resource.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ResourceStatus? Status { get; set; }

    // Optional writable numeric property (default 0)
    [TypeProperty("Maximum number of retry attempts.")]
    public int MaxRetries { get; set; }

    // Optional writable numeric property (default 0)
    [TypeProperty("Timeout in seconds for operations.")]
    public int TimeoutSeconds { get; set; }

    // Optional writable nested object property
    [TypeProperty("Additional metadata for the resource.")]
    public ResourceMetadata? Metadata { get; set; }

    // Read-only output property
    [TypeProperty("The unique identifier of the resource.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ResourceId { get; set; }

    // Read-only output property (epoch milliseconds)
    [TypeProperty("The timestamp when the resource was created, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int CreatedAt { get; set; }

    // Read-only output property (epoch milliseconds)
    [TypeProperty("The timestamp when the resource was last updated, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int UpdatedAt { get; set; }
}

public class SampleResourceIdentifiers
{
    // Required identifier property
    [TypeProperty("The unique name of the resource.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }
}

public class ResourceMetadata
{
    [TypeProperty("The user or system that created the resource.")]
    public string? CreatedBy { get; set; }

    [TypeProperty("The version of the resource schema.")]
    public string? Version { get; set; }
}
