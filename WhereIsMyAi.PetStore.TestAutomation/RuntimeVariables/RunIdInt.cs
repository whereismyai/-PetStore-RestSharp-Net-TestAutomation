using System;

namespace WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;

/// <summary>
/// Runtime variable: run_id_int - a random 4-digit number generated once per scenario (by RunIdIntHooks).
/// </summary>
public class RunIdInt
{
    private int? _value;

    public int Value
    {
        get => _value ?? throw new InvalidOperationException("run_id_int has not been generated for this scenario yet.");
        set => _value = value;
    }
}
