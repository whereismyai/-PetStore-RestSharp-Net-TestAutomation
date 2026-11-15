using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RestSharp;
using WhereIsMyAi.RestSharpBase.Core.Http;

namespace WhereIsMyAi.RestSharpBase.Core.Tests.Http;

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
    public void Send_BodyLeavesUnsetPropertiesOutAndKeepsEmptyValues()
    {
        var handler = new FakeHttpHandler();

        CreateBuilder(handler).SendPost("/items", new ItemDto { Id = 1, Name = "", Tags = [] });

        handler.Request!.Method.Should().Be(HttpMethod.Post);
        handler.Request.RequestUri.Should().Be(new Uri($"{BaseUrl}/items"));
        handler.Request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        handler.RequestBody.Should().Be("""{"id":1,"name":"","tags":[]}""");
    }

    [Test]
    public void Send_BodyWithOnlyUnsetPropertiesIsAnEmptyObject()
    {
        var handler = new FakeHttpHandler();

        CreateBuilder(handler).SendPost("/items", new ItemDto());

        handler.RequestBody.Should().Be("{}");
    }

    [Test]
    public void Send_WithoutBodySendsNoContent()
    {
        var handler = new FakeHttpHandler();

        CreateBuilder(handler).SendGet("/items/42");

        handler.Request!.Method.Should().Be(HttpMethod.Get);
        handler.Request.RequestUri.Should().Be(new Uri($"{BaseUrl}/items/42"));
        handler.RequestBody.Should().BeNull();
    }

    [Test]
    public void WithHeaderAndWithQueryParameter_ChainAndAreSentWithTheDefaultHeaders()
    {
        var handler = new FakeHttpHandler();

        CreateBuilder(handler).WithHeader("X-Api-Key", "secret").WithQueryParameter("status", "sold").SendGet("/items");

        handler.Request!.RequestUri.Should().Be(new Uri($"{BaseUrl}/items?status=sold"));
        handler.Request.Headers.GetValues("X-Api-Key").Should().ContainSingle().Which.Should().Be("secret");
        handler.Request.Headers.GetValues("X-Default").Should().ContainSingle().Which.Should().Be("on");
    }

    [Test]
    public void Send_ReturnsErrorStatusCodesWithoutThrowing()
    {
        var handler = new FakeHttpHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("missing") });

        var response = CreateBuilder(handler).SendGet("/items/1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Should().Be("missing");
    }

    [Test]
    public void Send_ThrowsWhenThereIsNoHttpResponse()
    {
        var handler = new FakeHttpHandler(() => throw new HttpRequestException("connection refused"));

        var act = () => CreateBuilder(handler).SendGet("/items/1");

        act.Should().Throw<HttpRequestException>().WithMessage("*/items/1 got no response*");
    }

    [Test]
    public async Task SendAsync_ReturnsTheResponse()
    {
        var handler = new FakeHttpHandler(() => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}") });

        var response = await CreateBuilder(handler).SendPostAsync("/items", new ItemDto { Id = 1 }, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        handler.RequestBody.Should().Be("""{"id":1}""");
    }

    [Test]
    public async Task SendAsync_ThrowsWhenCancelled()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var act = () => CreateBuilder(new FakeHttpHandler()).SendPostAsync("/items", new ItemDto(), cancelled.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void InjectedLogger_ReceivesTheRequestBodyAsSent()
    {
        var logger = new ListLogger();

        CreateBuilder(new FakeHttpHandler(), logger).SendPost("/items", new ItemDto { Name = "" });

        logger.Messages.Should().Contain(message => message.Contains("""{"name":""}"""));
    }

    [Test]
    public void VerifyStatus_ReturnsTheSameResponseOnTheExpectedStatus()
    {
        var response = CreateBuilder(new FakeHttpHandler()).SendGet("/items/1");

        response.VerifyStatus(200).Should().BeSameAs(response);
    }

    [Test]
    public void VerifyStatus_ThrowsWithStatusAndBodyOnAnotherStatus()
    {
        var handler = new FakeHttpHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("missing") });
        var response = CreateBuilder(handler).SendGet("/items/1");

        var act = () => response.VerifyStatus(200);

        act.Should().Throw<InvalidOperationException>().WithMessage("Expected status 200 from Get /items/1, got 404. Body: 'missing'.");
    }

    private static TestBuilder CreateBuilder(FakeHttpHandler handler, ILogger? logger = null) =>
        new(new RestClient(new RestClientOptions(BaseUrl) { ConfigureMessageHandler = _ => handler }), logger);

    private sealed class TestBuilder(RestClient client, ILogger? logger) : RestApiClient<TestBuilder>(client, logger)
    {
        protected override IEnumerable<KeyValuePair<string, string>> DefaultHeaders => [new("X-Default", "on")];

        public RestResponse SendGet(string resource) => Send(Method.Get, resource);
        public RestResponse SendPost(string resource, object body) => Send(Method.Post, resource, body);

        public Task<RestResponse> SendPostAsync(string resource, object body, CancellationToken token) =>
            SendAsync(Method.Post, resource, body, token);
    }

    private sealed class ListLogger : ILogger
    {
        public List<string> Messages { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
