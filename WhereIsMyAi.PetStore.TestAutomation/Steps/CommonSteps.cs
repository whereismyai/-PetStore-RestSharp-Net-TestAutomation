using FluentAssertions;
using WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;
using Reqnroll;
using RestSharp;
using WhereIsMyAi.RestSharpBase.Core.Helpers;

namespace WhereIsMyAi.PetStore.TestAutomation.Steps;

/// <summary>Steps that apply to any endpoint.</summary>
[Binding]
public class CommonSteps(LastResponse lastResponse, RunIdInt runIdInt) : BaseStep(runIdInt)
{
    // Read when a step runs, not in the constructor: the When steps set Value after this class is created.
    private RestResponse Response => lastResponse.Value;

    [Then(@"^the response should have '(\d+)' status code$")]
    public void ThenTheResponseShouldHaveStatusCode(int statusCode)
    {
        ((int)Response.StatusCode).Should().Be(statusCode, "response body: {0}", Response.Content);
    }

    // The body's "message" field must equal it exactly, e.g. {"code":1,"type":"error","message":"Pet not found"}.
    [Then(@"^the response message should be '(.*)'$")]
    public void ThenTheResponseMessageShouldBe(string expectedMessage)
    {
        Response.GetJsonProperty("message").Should().Be(ReplaceVariables(expectedMessage), "response body: {0}", Response.Content);
    }

    // Media type only ('; charset=...' is ignored); RestSharp keeps Content-Type out of Response.Headers.
    [Then(@"^the response Content-Type should be '(.*)'$")]
    public void ThenTheResponseContentTypeShouldBe(string expectedContentType)
    {
        Response.ContentType.Should().Be(ReplaceVariables(expectedContentType), "response body: {0}", Response.Content);
    }
}
