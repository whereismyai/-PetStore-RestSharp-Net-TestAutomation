using System;
using System.Collections.Generic;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs.RuntimeVariables;
using WhereIsMyAi.RestSharpBase.Core.Helpers;

namespace WhereIsMyAi.PetStore.TestAutomation.Utils;

/// <summary>
/// The variables scenarios can use as placeholders, and how each one gets its value. Reqnroll DI creates
/// one instance per scenario, so the values always come from the current scenario's runtime variables.
/// The generic replacement logic lives in <see cref="VariableReplacer"/>.
/// </summary>
public class VariableRepository
{
    private readonly IReadOnlyDictionary<string, Func<string?, string>> _variables;

    public VariableRepository(RunIdInt runIdInt, CreatedPets createdPets)
    {
        _variables = new Dictionary<string, Func<string?, string>>
        {
            // {run_id_int} - random 4-digit number generated once per scenario
            ["run_id_int"] = _ => runIdInt.Value.ToString(),

            // {created-id} - Id the backend returned for the last created pet;
            // {created-id:2} - for the 2nd pet created in the scenario (1 = first)
            ["created-id"] = number => (number == null
                ? createdPets.LastId
                : createdPets.IdOf(int.Parse(number))).ToString(),
        };
    }

    public string Replace(string value) => value.ReplaceVariables(_variables);
}
