using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;
using WhereIsMyAi.RestSharpBase.Core.Http;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.Clients;

/// <summary>Business methods for the /pet endpoints. Reqnroll DI creates one instance per scenario.</summary>
public class PetApiClient() : RestApiClient(PetStoreConfig.BaseUrl)
{
    public RestResponse CreatePet(PetDto pet) => Post("/pet", pet);

    // String Ids, so tests can send invalid ones ("abc").
    public RestResponse GetPetById(string petId) => Get($"/pet/{petId}");

    public RestResponse DeletePetById(string petId) => Delete($"/pet/{petId}");
}
