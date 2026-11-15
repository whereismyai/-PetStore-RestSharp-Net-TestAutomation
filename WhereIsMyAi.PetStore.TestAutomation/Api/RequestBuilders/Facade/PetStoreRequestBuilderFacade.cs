namespace WhereIsMyAi.PetStore.TestAutomation.Api.RequestBuilders.Facade;

/// <summary>Single entry point for tests: one method per resource, each returns a new builder on the scenario's client.</summary>
public sealed class PetStoreRequestBuilderFacade(PetStoreApiClient client)
{
    public PetRequestBuilder Pet() => new(client);
}
