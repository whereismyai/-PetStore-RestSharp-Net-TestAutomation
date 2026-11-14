using WhereIsMyAi.RestSharpBase.Core.Helpers;

namespace WhereIsMyAi.PetStore.TestAutomation.Api;

public static class PetStoreConfig
{
    public static string BaseUrl => Settings.Get("BASE_URL");

    /// <summary>Local bug repository folder (BUGS_REPO_PATH, relative to the appsettings.json folder); null when not set.</summary>
    public static string? BugRepositoryPath => Settings.TryGetPath("BUGS_REPO_PATH");
}
