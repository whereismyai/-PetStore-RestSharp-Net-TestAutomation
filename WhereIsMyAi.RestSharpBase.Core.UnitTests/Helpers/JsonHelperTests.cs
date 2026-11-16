using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using NUnit.Framework;
using RestSharp;
using WhereIsMyAi.RestSharpBase.Core.Helpers;

namespace WhereIsMyAi.RestSharpBase.Core.UnitTests.Helpers;

public class JsonHelperTests
{
    private sealed class ItemDto
    {
        public long? Id { get; set; }
        public string? Name { get; set; }
        public List<string>? Tags { get; set; }
    }

    [Test]
    public void ContentAs_ReadsAValidBody()
    {
        var item = Response("""{"id":7,"name":"Rex","tags":[]}""").ContentAs<ItemDto>();

        item.Id.Should().Be(7);
        item.Name.Should().Be("Rex");
        item.Tags.Should().BeEmpty();
    }

    [Test]
    public void ContentAs_PropertyNamesAreCaseSensitive()
    {
        var item = Response("""{"ID":7,"NAME":"Rex","TAGS":[]}""").ContentAs<ItemDto>();

        item.Id.Should().BeNull("'ID' is not the contract's 'id'");
        item.Name.Should().BeNull();
        item.Tags.Should().BeNull();
    }

    [Test]
    public void ContentAs_PropertyMissingFromTheBodyStaysNull()
    {
        var item = Response("""{"id":7}""").ContentAs<ItemDto>();

        item.Name.Should().BeNull();
        item.Tags.Should().BeNull("a missing list must not turn into an empty one");
    }

    [TestCase(null, TestName = "ContentAs_Throws_NoBody")]
    [TestCase("", TestName = "ContentAs_Throws_EmptyBody")]
    [TestCase("null", TestName = "ContentAs_Throws_JsonNull")]
    [TestCase("<html>error</html>", TestName = "ContentAs_Throws_Html")]
    [TestCase("""{"id":7""", TestName = "ContentAs_Throws_TruncatedJson")]
    [TestCase("""[{"id":7}]""", TestName = "ContentAs_Throws_Array")]
    [TestCase("""{"id":"7"}""", TestName = "ContentAs_Throws_NumberSentAsString")]
    [TestCase("""{"tags":"one"}""", TestName = "ContentAs_Throws_StringInsteadOfArray")]
    public void ContentAs_ThrowsAReadableErrorWhenTheBodyIsNotTheDto(string? body)
    {
        var act = () => Response(body).ContentAs<ItemDto>();

        act.Should().Throw<InvalidOperationException>().WithMessage("Expected a ItemDto in the response body (status 200)*");
    }

    [Test]
    public void GetJsonProperty_ReadsAStringWithoutQuotesAndANumberAsWritten()
    {
        var response = Response("""{"code":1,"type":"error","message":"Pet \"Rex\" not found"}""");

        response.GetJsonProperty("message").Should().Be("Pet \"Rex\" not found");
        response.GetJsonProperty("code").Should().Be("1");
    }

    [TestCase(null, TestName = "GetJsonProperty_Throws_NoBody")]
    [TestCase("", TestName = "GetJsonProperty_Throws_EmptyBody")]
    [TestCase("<html>error</html>", TestName = "GetJsonProperty_Throws_Html")]
    [TestCase("""[{"message":"x"}]""", TestName = "GetJsonProperty_Throws_Array")]
    [TestCase("""{"msg":"x"}""", TestName = "GetJsonProperty_Throws_PropertyMissing")]
    public void GetJsonProperty_ThrowsAReadableErrorWhenTheBodyHasNoSuchProperty(string? body)
    {
        var act = () => Response(body).GetJsonProperty("message");

        act.Should().Throw<InvalidOperationException>().WithMessage("Expected a JSON object with 'message' in the response body (status 200)*");
    }

    [Test]
    public void ToJson_LeavesNullPropertiesOut_CamelCase()
    {
        JsonHelper.ToJson(new ItemDto { Id = 1, Name = "" }).Should().Be("""{"id":1,"name":""}""");
    }

    private static RestResponse Response(string? body) =>
        new(new RestRequest()) { Content = body, StatusCode = HttpStatusCode.OK };
}
