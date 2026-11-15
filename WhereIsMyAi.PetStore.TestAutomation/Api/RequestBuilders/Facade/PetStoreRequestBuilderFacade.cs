using System;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.RequestBuilders.Facade;

/// <summary>Single entry point for tests: one method per resource, each returns a new builder.
/// Owns the RestClient; Reqnroll DI creates one per scenario and disposes it afterwards.</summary>
public sealed class PetStoreRequestBuilderFacade : IDisposable
{
    private readonly RestClient _client = new(PetStoreConfig.BaseUrl);

    public PetRequestBuilder Pet() => new(_client);

    public void Dispose() => _client.Dispose();
}
