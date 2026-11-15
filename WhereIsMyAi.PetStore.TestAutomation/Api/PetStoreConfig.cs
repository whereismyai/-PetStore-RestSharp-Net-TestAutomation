using WhereIsMyAi.RestSharpBase.Core.Helpers;

namespace WhereIsMyAi.PetStore.TestAutomation.Api;

public static class PetStoreConfig
{
    public static string BaseUrl => EnvFileHelper.Get("BASE_URL");

    /// <summary>Local bug repository folder (BUGS_REPO_PATH, relative to the .env folder); null when not set.</summary>
    public static string? BugRepositoryPath => EnvFileHelper.GetOptionalPath("BUGS_REPO_PATH");
}
