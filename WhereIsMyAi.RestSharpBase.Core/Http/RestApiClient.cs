using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.RestSharpBase.Core.Helpers;
using WhereIsMyAi.RestSharpBase.Core.Logger;
using RestSharp;

namespace WhereIsMyAi.RestSharpBase.Core.Http;

/// <summary>
/// Thin fluent RestSharp wrapper; request builders derive from it (TBuilder = the derived builder, so WithX calls chain).
/// The RestClient (auth, timeouts, message handler) and logger are injected; whoever created the RestClient disposes it.
/// </summary>
public abstract class RestApiClient<TBuilder>(RestClient client, ILogger? logger = null)
    where TBuilder : RestApiClient<TBuilder>
{
    private readonly ILogger _log = logger ?? InternalLogger.Logger;
    private readonly List<KeyValuePair<string, string>> _headers = new();
    private readonly List<KeyValuePair<string, string>> _queryParameters = new();

    /// <summary>Headers every request of this builder sends (e.g. Accept); WithHeader adds to them.</summary>
    protected virtual IEnumerable<KeyValuePair<string, string>> DefaultHeaders => [];

    public TBuilder WithHeader(string name, string value)
    {
        _headers.Add(new(name, value));
        return (TBuilder)this;
    }

    public TBuilder WithQueryParameter(string name, string value)
    {
        _queryParameters.Add(new(name, value));
        return (TBuilder)this;
    }

    /// <summary>Sends the request; a body is serialized to JSON without its null properties. Throws when there is no HTTP response at all.</summary>
    protected RestResponse Send(Method method, string resource, object? body = null)
    {
        var request = BuildRequest(method, resource, body);
        return CheckResponse(request, client.Execute(request));
    }

    /// <summary>Async version of <see cref="Send"/>; a cancelled token throws OperationCanceledException.</summary>
    protected async Task<RestResponse> SendAsync(Method method, string resource, object? body = null,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(method, resource, body);
        var response = await client.ExecuteAsync(request, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return CheckResponse(request, response);
    }

    private RestRequest BuildRequest(Method method, string resource, object? body)
    {
        var request = new RestRequest(resource, method);
        foreach (var header in DefaultHeaders)
            request.AddHeader(header.Key, header.Value);
        foreach (var header in _headers)
            request.AddHeader(header.Key, header.Value);
        foreach (var parameter in _queryParameters)
            request.AddQueryParameter(parameter.Key, parameter.Value);

        if (body != null)
        {
            var json = JsonHelper.ToJson(body);
            request.AddStringBody(json, ContentType.Json);
            _log.LogInformation("{Method} {Resource} request body: {Body}", method, resource, json);
        }

        return request;
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
}
