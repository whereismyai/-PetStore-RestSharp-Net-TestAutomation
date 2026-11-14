using System;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs.RuntimeVariables;
using WhereIsMyAi.RestSharpBase.Core.Logger;
using Reqnroll;

namespace WhereIsMyAi.PetStore.TestAutomation.Hooks;

[Binding]
public class RunIdIntHooks
{
    private readonly RunIdInt _runIdInt;

    public RunIdIntHooks(RunIdInt runIdInt)
    {
        _runIdInt = runIdInt;
    }
    
    [BeforeScenario]
    public void GenerateRunIdInt()
    {
        _runIdInt.Value = Random.Shared.Next(1000, 10000); // always 4 digits, never a leading zero
        InternalLogger.Logger.LogInformation("run_id_int = {RunIdInt}", _runIdInt.Value);
    }
}
