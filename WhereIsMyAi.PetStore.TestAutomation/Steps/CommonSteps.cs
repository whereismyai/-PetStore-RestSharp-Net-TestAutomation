using FluentAssertions;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs.RuntimeVariables;
using Reqnroll;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Steps;

/// <summary>Steps that apply to any endpoint.</summary>
[Binding]
public class CommonSteps(LastResponse lastResponse)
{
    // Read when a step runs, not in the constructor: the When steps set Value after this class is created.
    private RestResponse Response => lastResponse.Value;

    [Then(@"the response status should be '(\d+)'")]
    public void ThenTheResponseStatusShouldBe(int expectedStatus)
    {
        ((int)Response.StatusCode).Should().Be(expectedStatus, "response body: {0}", Response.Content);
    }

    // Checks the "message" field of the API's generic reply, e.g. {"code":1,"type":"error","message":"Pet not found"},
    // straight on the raw body: the whole "message":"<expected>" pair must be present, so a partial value does not match.
    [Then(@"^the response message should be '(.*)'$")]
    public void ThenTheResponseMessageShouldBe(string expectedMessage)
    {
        Response.Content.Should().Contain($"\"message\":\"{expectedMessage}\"");
    }
}
