using Reqnroll;
using Reqnroll.UnitTestProvider;
using WhereIsMyAi.PetStore.TestAutomation.Api;
using WhereIsMyAi.RestSharpBase.Core.Tms;

namespace WhereIsMyAi.PetStore.TestAutomation.Hooks;

/// <summary>
/// Skips a scenario tagged @bug:&lt;id&gt; while that bug is Open in the local bug repository (BUGS_REPO_PATH).
/// The decision is Core's <see cref="BugGate"/>; this hook only performs the skip.
/// </summary>
[Binding]
public class BugHooks
{
    private readonly ScenarioContext _scenarioContext;
    private readonly IUnitTestRuntimeProvider _testRuntime;

    public BugHooks(ScenarioContext scenarioContext, IUnitTestRuntimeProvider testRuntime)
    {
        _scenarioContext = scenarioContext;
        _testRuntime = testRuntime;
    }

    // Order 0: decide before any other BeforeScenario hook does work for a scenario that will not run.
    [BeforeScenario(Order = 0)]
    public void SkipScenarioBlockedByOpenBug()
    {
        var reason = new BugGate(PetStoreConfig.BugRepositoryPath).GetSkipReason(_scenarioContext.ScenarioInfo.CombinedTags);
        if (reason != null)
            _testRuntime.TestIgnore(reason);
    }
}
