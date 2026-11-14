using System;
using System.Net;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.PetStore.TestAutomation.Api.Clients;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs.RuntimeVariables;
using WhereIsMyAi.RestSharpBase.Core.Logger;
using Reqnroll;

namespace WhereIsMyAi.PetStore.TestAutomation.Hooks;

[Binding]
public class CleanupHooks
{
    private readonly CreatedPets _createdPets;
    private readonly PetApiClient _petApi;

    public CleanupHooks(CreatedPets createdPets, PetApiClient petApi)
    {
        _createdPets = createdPets;
        _petApi = petApi;
    }

    // A failed cleanup must not fail the scenario - its assertions already ran on their own merit -
    // so failures are only written to the scenario's output as a warning line instead of an exception.
    // 404 is fine: the scenario already deleted that pet, which is the state cleanup wants anyway.
    [AfterScenario]
    public void DeleteCreatedPets()
    {
        foreach (var pet in _createdPets.Value)
        {
            try
            {
                var response = _petApi.DeletePetById(pet.Id!.Value.ToString());
                if (!response.IsSuccessful && response.StatusCode != HttpStatusCode.NotFound)
                {
                    throw new Exception($"status code {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Logged once: InternalLogger writes to the console on this thread, so the line lands in this
                // scenario's test report (and the console). Writing to TestContext as well would duplicate it.
                InternalLogger.Logger.LogWarning("Pet {PetId}: cleanup DELETE failed: {Error}", pet.Id, ex.Message);
            }
        }
    }
}
