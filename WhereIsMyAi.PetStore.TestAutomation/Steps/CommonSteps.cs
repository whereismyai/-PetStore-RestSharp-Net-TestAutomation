using FluentAssertions;
using WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;
using Reqnroll;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Steps;

/// <summary>Steps that apply to any endpoint.</summary>
[Binding]
public class CommonSteps(LastResponse lastResponse, RunIdInt runIdInt) : BaseStep(runIdInt)
{
    // Read when a step runs, not in the constructor: the When steps set Value after this class is created.
    private RestResponse Response => lastResponse.Value;

    [Then(@"^the response status should be '(\d+)'$")]
    public void ThenTheResponseStatusShouldBe(int expectedStatus)
    {
        ((int)Response.StatusCode).Should().Be(expectedStatus, "response body: {0}", Response.Content);
    }

    // The whole "message":"<expected>" pair must be in the body, e.g. {"code":1,"type":"error","message":"Pet not found"}.
    [Then(@"^the response message should be '(.*)'$")]
    public void ThenTheResponseMessageShouldBe(string expectedMessage)
    {
        Response.Content.Should().Contain($"\"message\":\"{ReplaceVariables(expectedMessage)}\"");
    }

    [Then(@"^the response body should be empty$")]
    public void ThenTheResponseBodyShouldBeEmpty()
    {
        Response.Content.Should().BeNullOrEmpty();
    }
}
