using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.RestSharpBase.Core.Logger;
using RestSharp;

namespace WhereIsMyAi.RestSharpBase.Core.Http;

/// <summary>Generic RestSharp wrapper. API clients derive from it and expose business methods.</summary>
public abstract class RestApiClient
{
    // One RestClient per base URL for the whole test run, as RestSharp recommends: a RestClient owns an
    // HttpClient and its connection pool, so creating one per API client instance wastes sockets. A RestClient
    // is thread-safe and holds no per-request state, so sharing it keeps no test data between scenarios.
    private static readonly ConcurrentDictionary<string, RestClient> Clients = new();

    private readonly RestClient _client;

    protected RestApiClient(string baseUrl)
    {
        _client = Clients.GetOrAdd(baseUrl, url => new RestClient(url));
    }

    private static ILogger Log => InternalLogger.Logger;

    protected RestResponse Get(string resource) =>
        Send(new RestRequest(resource, Method.Get));

    protected RestResponse Post<T>(string resource, T body) where T : class =>
        Send(new RestRequest(resource, Method.Post).AddJsonBody(body));

    protected RestResponse Put<T>(string resource, T body) where T : class =>
        Send(new RestRequest(resource, Method.Put).AddJsonBody(body));

    protected RestResponse Delete(string resource) =>
        Send(new RestRequest(resource, Method.Delete));

    private RestResponse Send(RestRequest request)
    {
        var response = _client.Execute(request);

        // Status 0 means no HTTP response at all (DNS, timeout, connection refused) - fail loudly
        // instead of letting a test assert against a fake status code.
        if (response.StatusCode == 0)
        {
            Log.LogError("{Method} {Resource} got no response: {Error}", request.Method, request.Resource, response.ErrorMessage);
            throw new HttpRequestException(
                $"{request.Method} {request.Resource} got no response: {response.ErrorMessage}",
                response.ErrorException);
        }

        Log.LogInformation("{Method} {Resource} -> {Status}", request.Method, request.Resource, (int)response.StatusCode);
        Log.LogDebug("Response body: {Body}", response.Content);

        return response;
    }
}
