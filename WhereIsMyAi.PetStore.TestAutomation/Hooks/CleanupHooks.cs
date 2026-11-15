using System;
using System.Net;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.PetStore.TestAutomation.Api.Clients;
using WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;
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

    // A failed cleanup only logs a warning, it never fails the scenario; 404 = already deleted by the scenario.
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
                InternalLogger.Logger.LogWarning("Pet {PetId}: cleanup DELETE failed: {Error}", pet.Id, ex.Message);
            }
        }
    }
}
