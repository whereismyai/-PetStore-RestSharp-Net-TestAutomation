using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;
using WhereIsMyAi.RestSharpBase.Core.Http;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.Clients;

/// <summary>Business methods for the /pet endpoints. Reqnroll DI creates one instance per scenario.</summary>
public class PetApiClient : RestApiClient
{
    public PetApiClient() : base(PetStoreConfig.BaseUrl)
    {
    }

    public RestResponse CreatePet(PetDto pet) => Post("/pet", pet);

    // IDs are strings, not longs: a test client must be able to send invalid IDs (e.g. "abc") too.
    public RestResponse GetPetById(string petId) => Get($"/pet/{petId}");

    public RestResponse DeletePetById(string petId) => Delete($"/pet/{petId}");
}
