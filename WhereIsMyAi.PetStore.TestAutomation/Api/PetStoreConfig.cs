using WhereIsMyAi.RestSharpBase.Core.Helpers;

namespace WhereIsMyAi.PetStore.TestAutomation.Api;

public static class PetStoreConfig
{
    public static string BaseUrl => EnvFile.Get("BASE_URL");
}
