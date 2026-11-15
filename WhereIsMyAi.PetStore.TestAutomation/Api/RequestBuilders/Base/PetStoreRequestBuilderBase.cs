using System.Collections.Generic;
using WhereIsMyAi.RestSharpBase.Core.Http;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.RequestBuilders.Base;

/// <summary>Base of every PetStore request builder: default headers.</summary>
public abstract class PetStoreRequestBuilderBase<TBuilder>(RestClient client) : RestApiClient<TBuilder>(client)
    where TBuilder : PetStoreRequestBuilderBase<TBuilder>
{
    protected override IEnumerable<KeyValuePair<string, string>> DefaultHeaders => [new("Accept", "application/json")];
}
