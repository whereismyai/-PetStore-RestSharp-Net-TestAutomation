using System;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.RestSharpBase.Core.Logger;

namespace WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;

/// <summary>Runtime variable behind {run_id_int}: a random 4-digit number, one per scenario (Reqnroll DI), never static, so parallel runs stay safe.</summary>
public class RunIdInt
{
    public int Value { get; } = Random.Shared.Next(1000, 10000); // always 4 digits, never a leading zero

    public RunIdInt() => InternalLogger.Logger.LogInformation("run_id_int = {RunIdInt}", Value);
}
