using System;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using RestSharp;
using WhereIsMyAi.RestSharpBase.Core.Helpers;

namespace WhereIsMyAi.RestSharpBase.Core.Tests.Helpers;

public class JsonHelperTests
{
    private sealed class ItemDto
    {
        public long? Id { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void ContentAs_ReadsAValidBody_CaseInsensitive()
    {
        var item = Response("""{"ID":7,"name":"Rex"}""").ContentAs<ItemDto>();

        item.Id.Should().Be(7);
        item.Name.Should().Be("Rex");
    }

    [TestCase(null, TestName = "ContentAs_Throws_NoBody")]
    [TestCase("", TestName = "ContentAs_Throws_EmptyBody")]
    [TestCase("null", TestName = "ContentAs_Throws_JsonNull")]
    [TestCase("<html>error</html>", TestName = "ContentAs_Throws_Html")]
    [TestCase("""{"id":7""", TestName = "ContentAs_Throws_TruncatedJson")]
    [TestCase("""[{"id":7}]""", TestName = "ContentAs_Throws_Array")]
    [TestCase("""{"id":"seven"}""", TestName = "ContentAs_Throws_WrongJsonType")]
    public void ContentAs_ThrowsAReadableErrorWhenTheBodyIsNotTheDto(string? body)
    {
        var act = () => Response(body).ContentAs<ItemDto>();

        act.Should().Throw<InvalidOperationException>().WithMessage("Expected a ItemDto in the response body (status 200)*");
    }

    [Test]
    public void ContentAsJsonObject_KeepsPropertyPresenceAndJsonTypes()
    {
        var json = Response("""{"id":7,"name":null}""").ContentAsJsonObject();

        json["id"]!.GetValueKind().Should().Be(JsonValueKind.Number);
        json.ContainsKey("name").Should().BeTrue();
        json["name"].Should().BeNull();
        json.ContainsKey("tags").Should().BeFalse();
    }

    [Test]
    public void ToJson_WritesDtoNullsAndUsesThePropertyNames()
    {
        JsonHelper.ToJson(new ItemDto { Id = 1 }).Should().Be("""{"Id":1,"Name":null}""");
    }

    private static RestResponse Response(string? body) =>
        new(new RestRequest()) { Content = body, StatusCode = HttpStatusCode.OK };
}
