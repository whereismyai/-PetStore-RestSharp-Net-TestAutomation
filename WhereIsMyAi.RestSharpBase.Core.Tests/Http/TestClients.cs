using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RestSharp;
using WhereIsMyAi.RestSharpBase.Core.Http;

namespace WhereIsMyAi.RestSharpBase.Core.Tests.Http;

/// <summary>A client on a fake handler, with one default header.</summary>
public static class TestClients
{
    public const string BaseUrl = "http://api.test";
    public const string DefaultHeader = "X-Default";

    public static RestApiClient Create(FakeHttpHandler handler, ILogger? logger = null) =>
        new TestClient(new RestClient(new RestClientOptions(BaseUrl) { ConfigureMessageHandler = _ => handler }), logger);

    private sealed class TestClient(RestClient client, ILogger? logger) : RestApiClient(client, logger)
    {
        protected override IEnumerable<KeyValuePair<string, string>> DefaultHeaders => [new(DefaultHeader, "default")];
    }
}
