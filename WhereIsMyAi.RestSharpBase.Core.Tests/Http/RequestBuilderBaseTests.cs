using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using RestSharp;
using WhereIsMyAi.RestSharpBase.Core.Http;

namespace WhereIsMyAi.RestSharpBase.Core.Tests.Http;

public class RequestBuilderBaseTests
{
    private sealed class ItemDto
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    }

    [Test]
    public void Send_BodyLeavesUnsetPropertiesOutAndKeepsEmptyValues()
    {
        var handler = new FakeHttpHandler();

        Builder(handler).SendPost("/items", new ItemDto { Id = 1, Name = "", Tags = [] });

        handler.Request!.Method.Should().Be(HttpMethod.Post);
        handler.Request.RequestUri.Should().Be(new Uri($"{TestClients.BaseUrl}/items"));
        handler.Request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        handler.RequestBody.Should().Be("""{"id":1,"name":"","tags":[]}""");
    }

    [Test]
    public void Send_BodyWithOnlyUnsetPropertiesIsAnEmptyObject()
    {
        var handler = new FakeHttpHandler();

        Builder(handler).SendPost("/items", new ItemDto());

        handler.RequestBody.Should().Be("{}");
    }

    [Test]
    public void Send_WithoutBodySendsNoContent()
    {
        var handler = new FakeHttpHandler();

        Builder(handler).SendGet("/items/42");

        handler.Request!.Method.Should().Be(HttpMethod.Get);
        handler.Request.RequestUri.Should().Be(new Uri($"{TestClients.BaseUrl}/items/42"));
        handler.RequestBody.Should().BeNull();
    }

    [Test]
    public void WithHeaderAndWithQueryParameter_ChainAndAreSentWithTheClientDefaults()
    {
        var handler = new FakeHttpHandler();

        Builder(handler).WithHeader("X-Api-Key", "secret").WithQueryParameter("status", "sold").SendGet("/items");

        handler.Request!.RequestUri.Should().Be(new Uri($"{TestClients.BaseUrl}/items?status=sold"));
        handler.Request.Headers.GetValues("X-Api-Key").Should().ContainSingle().Which.Should().Be("secret");
        handler.Request.Headers.GetValues(TestClients.DefaultHeader).Should().ContainSingle().Which.Should().Be("default");
    }

    [Test]
    public async Task SendAsync_SendsTheBody()
    {
        var handler = new FakeHttpHandler();

        await Builder(handler).SendPostAsync("/items", new ItemDto { Id = 1 }, CancellationToken.None);

        handler.RequestBody.Should().Be("""{"id":1}""");
    }

    private static TestBuilder Builder(FakeHttpHandler handler) => new(TestClients.Create(handler));

    private sealed class TestBuilder(RestApiClient client) : RequestBuilderBase<TestBuilder>(client)
    {
        public RestResponse SendGet(string resource) => Send(Method.Get, resource);
        public RestResponse SendPost(string resource, object body) => Send(Method.Post, resource, body);

        public Task<RestResponse> SendPostAsync(string resource, object body, CancellationToken token) =>
            SendAsync(Method.Post, resource, body, token);
    }
}
