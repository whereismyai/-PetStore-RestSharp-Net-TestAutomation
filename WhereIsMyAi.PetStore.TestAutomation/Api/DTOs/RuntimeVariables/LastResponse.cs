using System;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.DTOs.RuntimeVariables;

/// <summary>
/// Runtime context variable: the response of the last request sent in the current scenario.
/// Used for the verification of the outcome after api request is sent
/// </summary>
public class LastResponse
{
    private RestResponse? _value;

    public RestResponse Value
    {
        get => _value ?? throw new InvalidOperationException("No request has been sent in this scenario yet.");
        set => _value = value;
    }
}
