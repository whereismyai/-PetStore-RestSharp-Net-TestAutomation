using System;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;

/// <summary>Runtime variable: the response of the last request sent in the current scenario.</summary>
public class LastResponse
{
    private RestResponse? _value;

    public RestResponse Value
    {
        get => _value ?? throw new InvalidOperationException("No request has been sent in this scenario yet.");
        set => _value = value;
    }
}
