using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.RestSharpBase.Core.Logger;
using RestSharp;

namespace WhereIsMyAi.RestSharpBase.Core.Http;

/// <summary>Generic RestSharp wrapper. API clients derive from it and expose business methods.</summary>
public abstract class RestApiClient : IDisposable
{
    private readonly RestClient _client;

    protected RestApiClient(string baseUrl)
    {
        _client = new RestClient(baseUrl);
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

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
