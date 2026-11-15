using System;
using System.Collections.Generic;
using System.Net;
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
        public List<string>? Tags { get; set; }
    }

    [Test]
    public void ToModel_ReadsAValidBody_CaseInsensitive()
    {
        var item = Response("""{"ID":7,"name":"Rex","tags":[]}""").ToModel<ItemDto>();

        item.Id.Should().Be(7);
        item.Name.Should().Be("Rex");
        item.Tags.Should().BeEmpty();
    }

    [Test]
    public void ToModel_PropertyMissingFromTheBodyStaysNull()
    {
        var item = Response("""{"id":7}""").ToModel<ItemDto>();

        item.Name.Should().BeNull();
        item.Tags.Should().BeNull("a missing list must not turn into an empty one");
    }

    [TestCase(null, TestName = "ToModel_Throws_NoBody")]
    [TestCase("", TestName = "ToModel_Throws_EmptyBody")]
    [TestCase("null", TestName = "ToModel_Throws_JsonNull")]
    [TestCase("<html>error</html>", TestName = "ToModel_Throws_Html")]
    [TestCase("""{"id":7""", TestName = "ToModel_Throws_TruncatedJson")]
    [TestCase("""[{"id":7}]""", TestName = "ToModel_Throws_Array")]
    [TestCase("""{"id":"seven"}""", TestName = "ToModel_Throws_WrongJsonType")]
    [TestCase("""{"tags":"one"}""", TestName = "ToModel_Throws_StringInsteadOfArray")]
    public void ToModel_ThrowsAReadableErrorWhenTheBodyIsNotTheDto(string? body)
    {
        var act = () => Response(body).ToModel<ItemDto>();

        act.Should().Throw<InvalidOperationException>().WithMessage("Expected a ItemDto in the response body (status 200)*");
    }

    [Test]
    public void ToJson_LeavesNullPropertiesOut()
    {
        JsonHelper.ToJson(new ItemDto { Id = 1, Name = "" }).Should().Be("""{"Id":1,"Name":""}""");
    }

    private static RestResponse Response(string? body) =>
        new(new RestRequest()) { Content = body, StatusCode = HttpStatusCode.OK };
}
