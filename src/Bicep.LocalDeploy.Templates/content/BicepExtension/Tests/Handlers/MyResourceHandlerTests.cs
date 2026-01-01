using Microsoft.Extensions.Logging;
using MyExtension.Handlers.SampleHandler;
using MyExtension.Models.SampleResource;

namespace MyExtension.Tests.Handlers;

/// <summary>
/// Unit tests for SampleResourceHandler.
/// These tests demonstrate testing patterns for Bicep Local Deploy resource handlers.
///
/// NOTE: These are example tests showing the testing patterns. For a real implementation,
/// you would need to mock HTTP calls or abstract the API layer into a testable service.
/// See the Bicep Local Deploy Unit Testing Guide for dependency injection patterns:
/// https://github.com/Azure/bicep/blob/main/docs/experimental/local-deploy-dotnet-unittesting-guide.md
/// </summary>
[TestClass]
public class SampleResourceHandlerTests
{
    private Mock<ILogger<SampleResourceHandler>>? _mockLogger;
    private SampleResourceHandler? _handler;

    [TestInitialize]
    public void Setup()
    {
        // Initialize mock logger
        _mockLogger = new Mock<ILogger<SampleResourceHandler>>();

        // Create the handler with mocked logger
        _handler = new SampleResourceHandler(_mockLogger.Object);
    }

    /// <summary>
    /// Example test demonstrating resource creation with required properties.
    /// This is a simple unit test that doesn't require external dependencies.
    /// </summary>
    [TestMethod]
    public void SampleResource_WithRequiredName_CreatesValidInstance()
    {
        // Arrange & Act
        var properties = new SampleResource
        {
            Name = "test-resource",
            Description = "Test description",
            IsEnabled = true,
        };

        // Assert
        properties.Should().NotBeNull();
        properties.Name.Should().Be("test-resource");
        properties.Description.Should().Be("Test description");
        properties.IsEnabled.Should().BeTrue();
    }

    /// <summary>
    /// Example test with data-driven approach.
    /// Tests that SampleResource handles various inputs correctly.
    /// </summary>
    [TestMethod]
    [DataRow("simple-name", "Simple Name")]
    [DataRow("complex-name-123", "Complex Name")]
    [DataRow("unicode-name", "Unicode: 你好")]
    public void SampleResource_WithVariousInputs_HandlesCorrectly(string name, string description)
    {
        // Arrange & Act
        var properties = new SampleResource { Name = name, Description = description };

        // Assert
        properties.Should().NotBeNull();
        properties.Name.Should().Be(name);
        properties.Description.Should().Be(description);
    }

    /// <summary>
    /// Example test demonstrating property validation.
    /// </summary>
    [TestMethod]
    public void SampleResource_WithRequiredProperties_IsValid()
    {
        // Arrange & Act
        var resource = new SampleResource
        {
            Name = "test-resource",
            Description = "Test description",
            IsEnabled = true,
            Status = ResourceStatus.Active,
            MaxRetries = 3,
            TimeoutSeconds = 30,
        };

        // Assert
        resource.Name.Should().Be("test-resource");
        resource.IsEnabled.Should().BeTrue();
        resource.Status.Should().Be(ResourceStatus.Active);
        resource.MaxRetries.Should().Be(3);
        resource.TimeoutSeconds.Should().Be(30);
    }

    /// <summary>
    /// Example test for enum values.
    /// </summary>
    [TestMethod]
    [DataRow(ResourceStatus.Active)]
    [DataRow(ResourceStatus.Inactive)]
    [DataRow(ResourceStatus.Pending)]
    [DataRow(ResourceStatus.Deleted)]
    public void SampleResource_WithValidStatus_AcceptsAllEnumValues(ResourceStatus status)
    {
        // Arrange & Act
        var resource = new SampleResource { Name = "test", Status = status };

        // Assert
        resource.Status.Should().Be(status);
    }
}
