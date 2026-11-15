using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WhereIsMyAi.RestSharpBase.Core.Helpers;
using RestSharp;

namespace WhereIsMyAi.RestSharpBase.Core.Http;

/// <summary>
/// Fluent base for request builders (TBuilder = the derived builder, so WithX calls chain). Builds the request -
/// headers, query parameters, a JSON body without its null properties - and sends it through the injected client.
/// </summary>
public abstract class RequestBuilderBase<TBuilder>(RestApiClient client)
    where TBuilder : RequestBuilderBase<TBuilder>
{
    private readonly List<KeyValuePair<string, string>> _headers = new();
    private readonly List<KeyValuePair<string, string>> _queryParameters = new();

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

    protected RestResponse Send(Method method, string resource, object? body = null) =>
        client.Send(BuildRequest(method, resource, body));

    protected Task<RestResponse> SendAsync(Method method, string resource, object? body = null,
        CancellationToken cancellationToken = default) =>
        client.SendAsync(BuildRequest(method, resource, body), cancellationToken);

    private RestRequest BuildRequest(Method method, string resource, object? body)
    {
        var request = new RestRequest(resource, method);
        foreach (var header in _headers)
            request.AddHeader(header.Key, header.Value);
        foreach (var parameter in _queryParameters)
            request.AddQueryParameter(parameter.Key, parameter.Value);
        if (body != null)
            request.AddStringBody(JsonHelper.ToJson(body), ContentType.Json);

        return request;
    }
}
