using FluentAssertions;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs.RuntimeVariables;
using WhereIsMyAi.PetStore.TestAutomation.Utils;
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

    // Checks the "message" field of the API's generic reply, e.g. {"code":1,"type":"error","message":"Pet not found"},
    // straight on the raw body: the whole "message":"<expected>" pair must be present, so a partial value does not match.
    // Recommended: when the expected message contains an Id, write it as on creation - '{run_id_int}12345'.
    [Then(@"^the response message should be '(.*)'$")]
    public void ThenTheResponseMessageShouldBe(string expectedMessage)
    {
        Response.Content.Should().Contain($"\"message\":\"{ReplaceVariables(expectedMessage)}\"");
    }

    // For replies that carry no message at all, e.g. DELETE of a pet that does not exist.
    [Then(@"^the response body should be empty$")]
    public void ThenTheResponseBodyShouldBeEmpty()
    {
        Response.Content.Should().BeNullOrEmpty();
    }
}
