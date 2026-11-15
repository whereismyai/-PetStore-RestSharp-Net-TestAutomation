using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
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

    [Test]
    public void Post_SendsJsonObjectExactlyAsBuilt_NullKeptAndOmittedPropertyAbsent()
    {
        var handler = new FakeHttpHandler();
        using var client = CreateClient(handler);

        client.CallPost("/pet", new JsonObject { ["id"] = 1, ["name"] = null, ["tags"] = new JsonArray() });

        handler.Request!.Method.Should().Be(HttpMethod.Post);
        handler.Request.RequestUri.Should().Be(new Uri($"{BaseUrl}/pet"));
        handler.Request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        handler.RequestBody.Should().Be("""{"id":1,"name":null,"tags":[]}""");
    }

    [Test]
    public void Get_SendsNoBody()
    {
        var handler = new FakeHttpHandler();
        using var client = CreateClient(handler);

        client.CallGet("/pet/42");

        handler.Request!.Method.Should().Be(HttpMethod.Get);
        handler.Request.RequestUri.Should().Be(new Uri($"{BaseUrl}/pet/42"));
        handler.RequestBody.Should().BeNull();
    }

    [Test]
    public void Execute_SendsCustomHeadersAndQueryParameters()
    {
        var handler = new FakeHttpHandler();
        using var client = CreateClient(handler);

        client.CallExecute(new RestRequest("/pet/findByStatus").AddHeader("X-Api-Key", "secret").AddQueryParameter("status", "sold"));

        handler.Request!.RequestUri.Should().Be(new Uri($"{BaseUrl}/pet/findByStatus?status=sold"));
        handler.Request.Headers.GetValues("X-Api-Key").Should().ContainSingle().Which.Should().Be("secret");
    }

    [Test]
    public void Execute_ReturnsErrorStatusCodesWithoutThrowing()
    {
        var handler = new FakeHttpHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("missing") });
        using var client = CreateClient(handler);

        var response = client.CallGet("/pet/1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Should().Be("missing");
    }

    [Test]
    public void Execute_ThrowsWhenThereIsNoHttpResponse()
    {
        var handler = new FakeHttpHandler(() => throw new HttpRequestException("connection refused"));
        using var client = CreateClient(handler);

        var act = () => client.CallGet("/pet/1");

        act.Should().Throw<HttpRequestException>().WithMessage("*/pet/1 got no response*");
    }

    [Test]
    public async Task ExecuteAsync_ReturnsTheResponse()
    {
        var handler = new FakeHttpHandler(() => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}") });
        using var client = CreateClient(handler);

        var response = await client.CallExecuteAsync(new RestRequest("/pet", Method.Post), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        handler.Request!.Method.Should().Be(HttpMethod.Post);
    }

    [Test]
    public async Task ExecuteAsync_ThrowsWhenCancelled()
    {
        using var client = CreateClient(new FakeHttpHandler());
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var act = () => client.CallExecuteAsync(new RestRequest("/pet"), cancelled.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void InjectedLogger_ReceivesTheRequestBodyAsSent()
    {
        var logger = new ListLogger();
        using var client = CreateClient(new FakeHttpHandler(), logger);

        client.CallPost("/pet", new JsonObject { ["name"] = "" });

        logger.Messages.Should().Contain(message => message.Contains("""{"name":""}"""));
    }

    private static TestClient CreateClient(FakeHttpHandler handler, ILogger? logger = null) =>
        new(new RestClient(new RestClientOptions(BaseUrl) { ConfigureMessageHandler = _ => handler }), logger);

    private sealed class TestClient(RestClient client, ILogger? logger) : RestApiClient(client, logger)
    {
        public RestResponse CallGet(string resource) => Get(resource);
        public RestResponse CallPost(string resource, object body) => Post(resource, body);
        public RestResponse CallExecute(RestRequest request) => Execute(request);
        public Task<RestResponse> CallExecuteAsync(RestRequest request, CancellationToken token) => ExecuteAsync(request, token);
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
