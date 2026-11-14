using WhereIsMyAi.PetStore.TestAutomation.Utils;

namespace WhereIsMyAi.PetStore.TestAutomation.Steps;

/// <summary>
/// Base for step classes: gives access to scenario state. Contains no step definitions -
/// Reqnroll would register inherited steps once per subclass and make them ambiguous.
/// </summary>
public abstract class BaseStep
{
    private readonly VariableRepository _variables;

    protected BaseStep(VariableRepository variables)
    {
        _variables = variables;
    }

    /// <summary>
    /// Call on every string value that comes from the scenario before it is used in a request or an
    /// assertion: replaces variable placeholders ({run_id_int}, {created-id:N}, ...) with their values.
    /// </summary>
    protected string ReplaceVariables(string value) => _variables.Replace(value);
}
