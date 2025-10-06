using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bicep.Local.Extension.Host.Handlers;
using Microsoft.Extensions.Logging;
using MyExtension.Models;

namespace MyExtension.Handlers;

/// <summary>
/// Base class for all resource handlers in MyExtension.
/// Provides common functionality for making REST API calls.
/// </summary>
/// <typeparam name="TProps">The resource properties type</typeparam>
/// <typeparam name="TIdentifiers">The resource identifiers type</typeparam>
public abstract class ResourceHandlerBase<TProps, TIdentifiers>
    : TypedResourceHandler<TProps, TIdentifiers, Configuration>
    where TProps : class
    where TIdentifiers : class
{
    protected readonly ILogger _logger;

    protected ResourceHandlerBase(ILogger logger)
    {
        _logger = logger;
    }

    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    /// <summary>
    /// Creates an HTTP client configured for API calls.
    /// </summary>
    /// <param name="configuration">The extension configuration</param>
    /// <returns>A configured HttpClient instance</returns>
    protected static HttpClient CreateClient(Configuration configuration)
    {
        var client = new HttpClient { BaseAddress = new Uri(configuration.BaseUrl.TrimEnd('/')) };
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
        );

        // Try API_KEY environment variable for authentication
        var apiKey = Environment.GetEnvironmentVariable("API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                apiKey
            );
        }

        return client;
    }

    /// <summary>
    /// Makes a REST API call and returns the deserialized response.
    /// </summary>
    /// <typeparam name="T">The response type to deserialize</typeparam>
    /// <param name="configuration">The extension configuration</param>
    /// <param name="method">The HTTP method to use</param>
    /// <param name="relativePath">The relative API path</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="payload">Optional request payload</param>
    /// <returns>The deserialized response</returns>
    protected async Task<T?> CallApiForResponse<T>(
        Configuration configuration,
        HttpMethod method,
        string relativePath,
        CancellationToken ct,
        object? payload = null
    )
    {
        using var client = CreateClient(configuration);

        var requestUri = $"{configuration.BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";

        _logger.LogInformation("Making {Method} request to {Uri}", method, requestUri);

        HttpResponseMessage response;
        if (payload is not null)
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (method == HttpMethod.Post)
            {
                response = await client.PostAsync(requestUri, content, ct);
            }
            else if (method == HttpMethod.Put)
            {
                response = await client.PutAsync(requestUri, content, ct);
            }
            else if (method == HttpMethod.Patch)
            {
                var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
                {
                    Content = content,
                };
                response = await client.SendAsync(request, ct);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported HTTP method with payload: {method}"
                );
            }
        }
        else
        {
            if (method == HttpMethod.Get)
            {
                response = await client.GetAsync(requestUri, ct);
            }
            else if (method == HttpMethod.Delete)
            {
                response = await client.DeleteAsync(requestUri, ct);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported HTTP method without payload: {method}"
                );
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "API call failed: {StatusCode} {Reason} Body={Body}",
                (int)response.StatusCode,
                response.ReasonPhrase,
                body
            );
            throw new InvalidOperationException(
                $"API call failed: {(int)response.StatusCode} {response.ReasonPhrase} Body={body}"
            );
        }

        if (typeof(T) == typeof(object) || response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("Response: {Response}", responseBody);

        return JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
    }

    /// <summary>
    /// Makes a REST API call without expecting a response body.
    /// </summary>
    /// <param name="configuration">The extension configuration</param>
    /// <param name="method">The HTTP method to use</param>
    /// <param name="relativePath">The relative API path</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="payload">Optional request payload</param>
    protected async Task CallApi(
        Configuration configuration,
        HttpMethod method,
        string relativePath,
        CancellationToken ct,
        object? payload = null
    )
    {
        await CallApiForResponse<object>(configuration, method, relativePath, ct, payload);
    }
}
