using System;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using NUnit.Framework;
using RestSharp;
using WhereIsMyAi.RestSharpBase.Core.Http;

namespace WhereIsMyAi.RestSharpBase.Core.Tests.Http;

public class RestResponseExtensionsTests
{
    [Test]
    public void VerifyStatus_ReturnsTheSameResponseOnTheExpectedStatus()
    {
        using var client = TestClients.Create(new FakeHttpHandler());
        var response = client.Send(new RestRequest("/items/1"));

        response.VerifyStatus(200).Should().BeSameAs(response);
    }

    [Test]
    public void VerifyStatus_ThrowsWithStatusAndBodyOnAnotherStatus()
    {
        var handler = new FakeHttpHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("missing") });
        using var client = TestClients.Create(handler);
        var response = client.Send(new RestRequest("/items/1"));

        var act = () => response.VerifyStatus(200);

        act.Should().Throw<InvalidOperationException>().WithMessage("Expected status 200 from Get /items/1, got 404. Body: 'missing'.");
    }
}
