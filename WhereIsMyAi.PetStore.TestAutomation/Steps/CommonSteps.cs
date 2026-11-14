using FluentAssertions;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;
using WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;
using WhereIsMyAi.PetStore.TestAutomation.Utils;
using WhereIsMyAi.RestSharpBase.Core.Helpers;
using Reqnroll;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Steps;

/// <summary>Steps that apply to any endpoint.</summary>
[Binding]
public class CommonSteps(LastResponse lastResponse, VariableRepository variables) : BaseStep(variables)
{
    // Read when a step runs, not in the constructor: the When steps set Value after this class is created.
    private RestResponse Response => lastResponse.Value;

    [Then(@"the response status should be '(\d+)'")]
    public void ThenTheResponseStatusShouldBe(int expectedStatus)
    {
        ((int)Response.StatusCode).Should().Be(expectedStatus, "response body: {0}", Response.Content);
    }

    // Checks the "message" field of the API's generic reply, e.g. {"code":1,"type":"error","message":"Pet not found"}.
    // The body is deserialized, so the whole message must be equal - a partial value does not match.
    // Recommended: when the expected message contains an Id, write it as on creation - '{run_id_int}12345'.
    [Then(@"^the response message should be '(.*)'$")]
    public void ThenTheResponseMessageShouldBe(string expectedMessage)
    {
        var reply = Response.Content<ApiResponseDto>();
        reply.Should().NotBeNull("a reply with a message was expected, response body: {0}", Response.Content);
        reply!.Message.Should().Be(ReplaceVariables(expectedMessage), "response body: {0}", Response.Content);
    }

    // For replies that carry no message at all, e.g. DELETE of a pet that does not exist.
    [Then(@"^the response body should be empty$")]
    public void ThenTheResponseBodyShouldBeEmpty()
    {
        Response.Content.Should().BeNullOrEmpty();
    }
}
