using System;
using RestSharp;

namespace WhereIsMyAi.RestSharpBase.Core.Http;

/// <summary>Chainable response checks: response.VerifyStatus(200).ToModel&lt;PetDto&gt;().</summary>
public static class RestResponseExtensions
{
    /// <summary>Returns the same response when its status is the expected one; throws with the status and body otherwise.</summary>
    public static RestResponse VerifyStatus(this RestResponse response, int expectedStatus) =>
        (int)response.StatusCode == expectedStatus
            ? response
            : throw new InvalidOperationException(
                $"Expected status {expectedStatus} from {response.Request.Method} {response.Request.Resource}, " +
                $"got {(int)response.StatusCode}. Body: '{response.Content}'.");
}
