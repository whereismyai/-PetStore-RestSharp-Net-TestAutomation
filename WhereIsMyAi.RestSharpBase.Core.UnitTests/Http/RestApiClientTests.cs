using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RestSharp;
using WhereIsMyAi.RestSharpBase.Core.Http;
using WhereIsMyAi.RestSharpBase.Core.UnitTests.Logger;

namespace WhereIsMyAi.RestSharpBase.Core.UnitTests.Http;

public class RestApiClientTests
{
    private const string BaseUrl = "http://api.test";

    private sealed class ItemDto
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    }

    [Test]
    public void Post_SendsJsonWithoutNullPropertiesAndKeepsEmptyValues()
    {
        var handler = new FakeHttpHandler();
        using var client = CreateClient(handler);

        client.CallPost("/items", new ItemDto { Id = 1, Name = "", Tags = [] });

        handler.Request!.Method.Should().Be(HttpMethod.Post);
        handler.Request.RequestUri.Should().Be(new Uri($"{BaseUrl}/items"));
        handler.Request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        handler.RequestBody.Should().Be("""{"id":1,"name":"","tags":[]}""");
    }

    [Test]
    public void Post_LeavesUnsetPropertiesOut()
    {
        var handler = new FakeHttpHandler();
        using var client = CreateClient(handler);

        client.CallPost("/items", new ItemDto { Name = "Rex" });

        handler.RequestBody.Should().Be("""{"name":"Rex"}""");
    }

    [Test]
    public void Get_SendsNoBody()
    {
        var handler = new FakeHttpHandler();
        using var client = CreateClient(handler);

        client.CallGet("/items/42");

        handler.Request!.Method.Should().Be(HttpMethod.Get);
        handler.Request.RequestUri.Should().Be(new Uri($"{BaseUrl}/items/42"));
        handler.RequestBody.Should().BeNull();
    }

    [Test]
    public void Execute_SendsCustomHeadersAndQueryParameters()
    {
        var handler = new FakeHttpHandler();
        using var client = CreateClient(handler);

        client.CallExecute(new RestRequest("/items").AddHeader("X-Api-Key", "secret").AddQueryParameter("status", "sold"));

        handler.Request!.RequestUri.Should().Be(new Uri($"{BaseUrl}/items?status=sold"));
        handler.Request.Headers.GetValues("X-Api-Key").Should().ContainSingle().Which.Should().Be("secret");
    }

    [Test]
    public void InjectedRestClient_DefaultHeadersAreSentWithEveryRequest()
    {
        var handler = new FakeHttpHandler();
        var restClient = new RestClient(new RestClientOptions(BaseUrl) { ConfigureMessageHandler = _ => handler });
        restClient.AddDefaultHeader("Authorization", "Bearer token");
        using var client = new TestClient(restClient, null);

        client.CallGet("/items");

        handler.Request!.Headers.Authorization!.ToString().Should().Be("Bearer token");
    }

    [Test]
    public void Execute_ReturnsErrorStatusCodesWithoutThrowing()
    {
        var handler = new FakeHttpHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("missing") });
        using var client = CreateClient(handler);

        var response = client.CallGet("/items/1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Should().Be("missing");
    }

    [Test]
    public void Execute_ThrowsWhenThereIsNoHttpResponse()
    {
        using var client = CreateClient(new FakeHttpHandler(() => throw new HttpRequestException("connection refused")));

        var act = () => client.CallGet("/items/1");

        act.Should().Throw<HttpRequestException>().WithMessage("*/items/1 got no response*");
    }

    [Test]
    public void InjectedLogger_ReceivesTheRequestBodyAsSentAndTheStatus()
    {
        var logger = new ListLogger();
        using var client = CreateClient(new FakeHttpHandler(), logger);

        client.CallPost("/items", new ItemDto { Name = "" });

        logger.Messages.Should().Contain(message => message.Contains("""{"name":""}"""))
            .And.Contain(message => message.Contains("/items -> 200"));
    }

    [Test]
    public void Dispose_DisposesTheInjectedRestClient()
    {
        var handler = new FakeHttpHandler();
        var client = CreateClient(handler);

        client.Dispose();

        handler.Disposed.Should().BeTrue("RestApiClient owns the RestClient it was given");
    }

    private static TestClient CreateClient(FakeHttpHandler handler, ILogger? logger = null) =>
        new(new RestClient(new RestClientOptions(BaseUrl) { ConfigureMessageHandler = _ => handler }), logger);

    private sealed class TestClient(RestClient client, ILogger? logger) : RestApiClient(client, logger)
    {
        public RestResponse CallGet(string resource) => Get(resource);
        public RestResponse CallPost(string resource, object body) => Post(resource, body);
        public RestResponse CallExecute(RestRequest request) => Execute(request);
    }
}
