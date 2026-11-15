using System.Collections.Generic;
using WhereIsMyAi.RestSharpBase.Core.Http;

namespace WhereIsMyAi.PetStore.TestAutomation.Api;

/// <summary>The PetStore HTTP client: BASE_URL and default headers. Reqnroll DI creates one per scenario and disposes it.</summary>
public sealed class PetStoreApiClient() : RestApiClient(PetStoreConfig.BaseUrl)
{
    protected override IEnumerable<KeyValuePair<string, string>> DefaultHeaders => [new("Accept", "application/json")];
}
