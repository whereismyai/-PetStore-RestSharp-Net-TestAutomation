using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
    [Test]
    public void Send_AddsTheDefaultHeaders()
    {
        var handler = new FakeHttpHandler();
        using var client = TestClients.Create(handler);

        client.Send(new RestRequest("/items"));

        handler.Request!.Headers.GetValues(TestClients.DefaultHeader).Should().ContainSingle().Which.Should().Be("default");
    }

    [Test]
    public void Send_HeaderSetOnTheRequestWinsOverTheDefault()
    {
        var handler = new FakeHttpHandler();
        using var client = TestClients.Create(handler);

        client.Send(new RestRequest("/items").AddHeader(TestClients.DefaultHeader, "custom"));

        handler.Request!.Headers.GetValues(TestClients.DefaultHeader).Should().ContainSingle().Which.Should().Be("custom");
    }

    [Test]
    public void Send_ReturnsErrorStatusCodesWithoutThrowing()
    {
        var handler = new FakeHttpHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("missing") });
        using var client = TestClients.Create(handler);

        var response = client.Send(new RestRequest("/items/1"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Should().Be("missing");
    }

    [Test]
    public void Send_ThrowsWhenThereIsNoHttpResponse()
    {
        using var client = TestClients.Create(new FakeHttpHandler(() => throw new HttpRequestException("connection refused")));

        var act = () => client.Send(new RestRequest("/items/1"));

        act.Should().Throw<HttpRequestException>().WithMessage("*/items/1 got no response*");
    }

    [Test]
    public async Task SendAsync_ReturnsTheResponse()
    {
        var handler = new FakeHttpHandler(() => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}") });
        using var client = TestClients.Create(handler);

        var response = await client.SendAsync(new RestRequest("/items", Method.Post), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        handler.Request!.Method.Should().Be(HttpMethod.Post);
    }

    [Test]
    public async Task SendAsync_ThrowsWhenCancelled()
    {
        using var client = TestClients.Create(new FakeHttpHandler());
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var act = () => client.SendAsync(new RestRequest("/items"), cancelled.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void InjectedLogger_ReceivesTheRequestBodyAndTheStatus()
    {
        var logger = new ListLogger();
        using var client = TestClients.Create(new FakeHttpHandler(), logger);

        client.Send(new RestRequest("/items", Method.Post).AddStringBody("""{"name":""}""", ContentType.Json));

        logger.Messages.Should().Contain(message => message.Contains("""{"name":""}"""))
            .And.Contain(message => message.Contains("/items -> 200"));
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
