using System;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.PetStore.TestAutomation.Api.Clients;
using WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;
using WhereIsMyAi.RestSharpBase.Core.Logger;
using Reqnroll;

namespace WhereIsMyAi.PetStore.TestAutomation.Hooks;

[Binding]
public class CleanupHooks(CreatedPets createdPets, PetApiClient petApi)
{
    private static ILogger Log => InternalLogger.Logger;

    // Deletes the Ids POST returned and the Ids sent; never fails the scenario, only warns (404 = already deleted).
    [AfterScenario]
    public void DeleteCreatedPets()
    {
        foreach (var pet in createdPets.Value.Where(pet => pet.Id == null))
            Log.LogWarning("Pet '{PetName}' came back without an Id - only a sent Id can be cleaned up", pet.Name);

        var petIds = createdPets.Value.Select(pet => pet.Id).OfType<long>().Concat(createdPets.SentIds).Distinct();
        foreach (var petId in petIds)
        {
            try
            {
                var response = petApi.DeletePetById(petId.ToString());
                if (!response.IsSuccessful && response.StatusCode != HttpStatusCode.NotFound)
                    Log.LogWarning("Pet {PetId}: cleanup DELETE returned {Status}", petId, (int)response.StatusCode);
            }
            catch (Exception ex) // no response at all (RestApiClient throws): still only a warning
            {
                Log.LogWarning("Pet {PetId}: cleanup DELETE failed: {Error}", petId, ex.Message);
            }
        }
    }
}
