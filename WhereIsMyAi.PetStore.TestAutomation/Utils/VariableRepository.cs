using System;
using System.Collections.Generic;
using WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;
using WhereIsMyAi.RestSharpBase.Core.Helpers;

namespace WhereIsMyAi.PetStore.TestAutomation.Utils;

/// <summary>
/// The variables scenarios can use as placeholders, and how each one gets its value. Reqnroll DI creates
/// one instance per scenario, so the values always come from the current scenario's runtime variables.
/// The generic replacement logic lives in <see cref="VariableReplacer"/>.
/// Backend-generated Ids need no variable: steps address those pets by name or as the last created pet.
/// </summary>
public class VariableRepository
{
    private readonly IReadOnlyDictionary<string, Func<string>> _variables;

    public VariableRepository(RunIdInt runIdInt)
    {
        _variables = new Dictionary<string, Func<string>>
        {
            // {run_id_int} - random 4-digit number generated once per scenario
            ["run_id_int"] = () => runIdInt.Value.ToString(),
        };
    }

    public string Replace(string value) => value.ReplaceVariables(_variables);
}
