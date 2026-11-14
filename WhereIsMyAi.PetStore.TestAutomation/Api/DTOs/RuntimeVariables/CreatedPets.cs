using System.Collections.Generic;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.DTOs.RuntimeVariables;

/// <summary>
/// Runtime context variable: pets created by the current scenario, in creation order, as the POST response
/// returned them. Reqnroll DI creates one instance per scenario; the AfterScenario hook removes every pet
/// listed here from the backend.
/// Deleted pets stay listed so a scenario can still refer to them (e.g. check that GET now returns 404).
/// </summary>
public class CreatedPets
{
    public CreatedPets()
    {
        Value = new List<PetDto>();
    }

    public List<PetDto> Value { get; set; }
}
