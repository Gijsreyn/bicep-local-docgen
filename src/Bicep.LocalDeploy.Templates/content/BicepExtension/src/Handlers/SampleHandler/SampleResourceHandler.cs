using Microsoft.Extensions.Logging;
using MyExtension.Models;
using MyExtension.Models.SampleResource;

namespace MyExtension.Handlers.SampleHandler;

/// <summary>
/// Handler for SampleResource operations.
/// Implements Preview, CreateOrUpdate, and Delete operations following the Bicep extension pattern.
/// </summary>
public class SampleResourceHandler
    : ResourceHandlerBase<SampleResource, SampleResourceIdentifiers>
{
    private const string ResourcesApiEndpoint = "api/v1/resources";

    public SampleResourceHandler(ILogger<SampleResourceHandler> logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Preview operation - checks if the resource exists and populates output properties.
    /// This method is called before CreateOrUpdate to determine the current state.
    /// </summary>
    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetResourceAsync(request.Config, request.Properties, cancellationToken);
        if (existing is not null)
        {
            // Populate output properties from existing resource
            request.Properties.ResourceId = existing.ResourceId;
            request.Properties.CreatedAt = existing.CreatedAt;
            request.Properties.UpdatedAt = existing.UpdatedAt;
        }
        return GetResponse(request);
    }

    /// <summary>
    /// CreateOrUpdate operation - creates a new resource or updates an existing one.
    /// This method handles both creation and update logic based on resource existence.
    /// </summary>
    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        
        _logger.LogInformation("Ensuring sample resource: {Name}", props.Name);

        var existing = await GetResourceAsync(request.Config, props, cancellationToken);

        if (existing is null)
        {
            _logger.LogInformation("Creating new sample resource: {Name}", props.Name);
            await CreateResourceAsync(request.Config, props, cancellationToken);
            existing = await GetResourceAsync(request.Config, props, cancellationToken)
                ?? throw new InvalidOperationException("Resource creation did not return resource.");
        }
        else
        {
            _logger.LogInformation("Updating existing sample resource: {ResourceId}", existing.ResourceId);
            await UpdateResourceAsync(request.Config, props, existing, cancellationToken);
            existing = await GetResourceAsync(request.Config, props, cancellationToken)
                ?? throw new InvalidOperationException("Resource update did not return resource.");
        }

        // Populate output properties
        props.ResourceId = existing.ResourceId;
        props.CreatedAt = existing.CreatedAt;
        props.UpdatedAt = existing.UpdatedAt;

        return GetResponse(request);
    }

    /// <summary>
    /// GetIdentifiers - extracts the identifier properties from the resource.
    /// These identifiers are used to locate and identify the resource.
    /// </summary>
    protected override SampleResourceIdentifiers GetIdentifiers(SampleResource properties) => new()
    {
        Name = properties.Name
    };

    private async Task<SampleResource?> GetResourceAsync(Configuration configuration, SampleResource props, CancellationToken ct)
    {
        try
        {
            var response = await CallApiForResponse<SampleResource>(
                configuration,
                HttpMethod.Get,
                $"{ResourcesApiEndpoint}/{Uri.EscapeDataString(props.Name)}",
                ct);

            return response;
        }
        catch
        {
            // Resource not found
            return null;
        }
    }

    private async Task CreateResourceAsync(Configuration configuration, SampleResource props, CancellationToken ct)
    {
        var createPayload = new
        {
            name = props.Name,
            description = props.Description,
            isEnabled = props.IsEnabled,
            status = props.Status?.ToString(),
            maxRetries = props.MaxRetries,
            timeoutSeconds = props.TimeoutSeconds,
            tags = props.Tags,
            metadata = props.Metadata
        };

        var response = await CallApiForResponse<SampleResource>(
            configuration,
            HttpMethod.Post,
            ResourcesApiEndpoint,
            ct,
            createPayload);

        if (response == null)
        {
            throw new InvalidOperationException($"Failed to create sample resource '{props.Name}'.");
        }
    }

    private async Task UpdateResourceAsync(Configuration configuration, SampleResource props, SampleResource existing, CancellationToken ct)
    {
        var updatePayload = new
        {
            description = props.Description,
            isEnabled = props.IsEnabled,
            status = props.Status?.ToString(),
            maxRetries = props.MaxRetries,
            timeoutSeconds = props.TimeoutSeconds,
            tags = props.Tags,
            metadata = props.Metadata
        };

        var response = await CallApiForResponse<SampleResource>(
            configuration,
            HttpMethod.Put,
            $"{ResourcesApiEndpoint}/{Uri.EscapeDataString(existing.Name)}",
            ct,
            updatePayload);

        if (response == null)
        {
            throw new InvalidOperationException($"Failed to update sample resource '{props.Name}'.");
        }
    }
}
