using WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;

namespace WhereIsMyAi.PetStore.TestAutomation.Steps;

/// <summary>
/// Base for step classes. Contains no step definitions -
/// Reqnroll would register inherited steps once per subclass and make them ambiguous.
/// </summary>
public abstract class BaseStep
{
    private readonly RunIdInt _runIdInt;

    protected BaseStep(RunIdInt runIdInt)
    {
        _runIdInt = runIdInt;
    }

    /// <summary>
    /// Call on every string value that comes from the scenario before it is used in a request or an
    /// assertion: replaces variables with their values; "{run_id_int}12345" -> "482112345".
    /// </summary>
    protected string ReplaceVariables(string value) =>
        value.Replace("{run_id_int}", _runIdInt.Value.ToString());
}
