using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.RestSharpBase.Core.Logger;
using RestSharp;

namespace WhereIsMyAi.RestSharpBase.Core.Http;

/// <summary>
/// The HTTP client: owns the RestClient (auth, timeouts, message handler) and the logger, adds default headers,
/// sends requests and logs them. Request builders (<see cref="RequestBuilderBase{TBuilder}"/>) send through it.
/// Derive to set a base URL and default headers per API.
/// </summary>
public class RestApiClient(RestClient client, ILogger? logger = null) : IDisposable
{
    private readonly ILogger _log = logger ?? InternalLogger.Logger;

    public RestApiClient(string baseUrl, ILogger? logger = null) : this(new RestClient(baseUrl), logger)
    {
    }

    /// <summary>Headers added to every request unless the request already sets them (e.g. Accept).</summary>
    protected virtual IEnumerable<KeyValuePair<string, string>> DefaultHeaders => [];

    /// <summary>Sends the request; throws when there is no HTTP response at all.</summary>
    public RestResponse Send(RestRequest request)
    {
        Prepare(request);
        return CheckResponse(request, client.Execute(request));
    }

    /// <summary>Async version of <see cref="Send"/>; a cancelled token throws OperationCanceledException.</summary>
    public async Task<RestResponse> SendAsync(RestRequest request, CancellationToken cancellationToken = default)
    {
        Prepare(request);
        var response = await client.ExecuteAsync(request, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return CheckResponse(request, response);
    }

    private void Prepare(RestRequest request)
    {
        foreach (var header in DefaultHeaders)
        {
            var isSet = request.Parameters.Any(parameter => parameter.Type == ParameterType.HttpHeader &&
                string.Equals(parameter.Name, header.Key, StringComparison.OrdinalIgnoreCase));
            if (!isSet)
                request.AddHeader(header.Key, header.Value);
        }

        if (request.Parameters.OfType<BodyParameter>().FirstOrDefault()?.Value is string body)
            _log.LogInformation("{Method} {Resource} request body: {Body}", request.Method, request.Resource, body);
    }

    private RestResponse CheckResponse(RestRequest request, RestResponse response)
    {
        // Status 0 means no HTTP response at all (DNS, timeout, connection refused) - fail loudly
        // instead of letting a test assert against a fake status code.
        if (response.StatusCode == 0)
        {
            _log.LogError("{Method} {Resource} got no response: {Error}", request.Method, request.Resource, response.ErrorMessage);
            throw new HttpRequestException(
                $"{request.Method} {request.Resource} got no response: {response.ErrorMessage}",
                response.ErrorException);
        }

        _log.LogInformation("{Method} {Resource} -> {Status}", request.Method, request.Resource, (int)response.StatusCode);
        _log.LogDebug("Response body: {Body}", response.Content);

        return response;
    }

    public void Dispose()
    {
        client.Dispose();
        GC.SuppressFinalize(this);
    }
}
