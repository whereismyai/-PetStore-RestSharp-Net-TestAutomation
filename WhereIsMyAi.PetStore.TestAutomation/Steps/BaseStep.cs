using WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;

namespace WhereIsMyAi.PetStore.TestAutomation.Steps;

/// <summary>Base for step classes. No step definitions here: Reqnroll would register them once per subclass (ambiguous).</summary>
public abstract class BaseStep(RunIdInt runIdInt)
{
    /// <summary>Call on every string value from the scenario before using it: "{run_id_int}12345" -> "482112345".</summary>
    protected string ReplaceVariables(string value) =>
        value.Replace("{run_id_int}", runIdInt.Value.ToString());
}
