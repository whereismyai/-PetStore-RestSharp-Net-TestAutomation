using System;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.RestSharpBase.Core.Logger;

namespace WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;

/// <summary>
/// Runtime variable: run_id_int - a random 4-digit number behind the {run_id_int} variable. Reqnroll DI creates
/// one instance per scenario, so every scenario has its own value; nothing is static, so parallel runs are safe.
/// </summary>
public class RunIdInt
{
    public int Value { get; } = Random.Shared.Next(1000, 10000); // always 4 digits, never a leading zero

    public RunIdInt() => InternalLogger.Logger.LogInformation("run_id_int = {RunIdInt}", Value);
}
