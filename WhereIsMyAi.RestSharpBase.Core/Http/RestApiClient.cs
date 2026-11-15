using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.RestSharpBase.Core.Helpers;
using WhereIsMyAi.RestSharpBase.Core.Logger;
using RestSharp;

namespace WhereIsMyAi.RestSharpBase.Core.Http;

/// <summary>
/// Generic RestSharp wrapper. API clients derive from it and expose business methods.
/// Configure without changing this class: pass your own RestClient (auth, default headers, timeouts) and logger,
/// and send anything the helpers do not cover (headers, query parameters, other bodies) through Execute.
/// </summary>
public abstract class RestApiClient(RestClient client, ILogger? logger = null) : IDisposable
{
    private readonly ILogger _log = logger ?? InternalLogger.Logger;

    protected RestApiClient(string baseUrl) : this(new RestClient(baseUrl))
    {
    }

    protected RestResponse Get(string resource) =>
        Execute(new RestRequest(resource, Method.Get));

    protected RestResponse Post<T>(string resource, T body) where T : class =>
        Execute(new RestRequest(resource, Method.Post).AddStringBody(JsonHelper.ToJson(body), ContentType.Json));

    protected RestResponse Put<T>(string resource, T body) where T : class =>
        Execute(new RestRequest(resource, Method.Put).AddStringBody(JsonHelper.ToJson(body), ContentType.Json));

    protected RestResponse Delete(string resource) =>
        Execute(new RestRequest(resource, Method.Delete));

    /// <summary>Sends any request and logs it; throws when there is no HTTP response at all.</summary>
    protected RestResponse Execute(RestRequest request)
    {
        if (request.Parameters.OfType<BodyParameter>().FirstOrDefault()?.Value is string body)
            _log.LogInformation("{Method} {Resource} request body: {Body}", request.Method, request.Resource, body);

        var response = client.Execute(request);

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
