using System;
using System.Net;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.PetStore.TestAutomation.Api.RequestBuilders.Facade;
using WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;
using WhereIsMyAi.RestSharpBase.Core.Logger;
using Reqnroll;

namespace WhereIsMyAi.PetStore.TestAutomation.Hooks;

[Binding]
public class CleanupHooks(CreatedPets createdPets, PetStoreRequestBuilderFacade petStore)
{
    private static ILogger Log => InternalLogger.Logger;

    // Cleanup never fails the scenario, it only logs a warning; 404 = already deleted by the scenario.
    [AfterScenario]
    public void DeleteCreatedPets()
    {
        foreach (var pet in createdPets.Value)
        {
            if (pet.Id == null)
            {
                Log.LogWarning("Pet '{PetName}' has no Id - nothing to clean up", pet.Name);
                continue;
            }

            try
            {
                var response = petStore.Pet().SendDeletePetById(pet.Id.Value.ToString());
                if (!response.IsSuccessful && response.StatusCode != HttpStatusCode.NotFound)
                    Log.LogWarning("Pet {PetId}: cleanup DELETE returned {Status}", pet.Id, (int)response.StatusCode);
            }
            catch (Exception ex) // no response at all (Core throws): still only a warning
            {
                Log.LogWarning("Pet {PetId}: cleanup DELETE failed: {Error}", pet.Id, ex.Message);
            }
        }
    }
}
