using Reqnroll;
using Reqnroll.UnitTestProvider;
using WhereIsMyAi.PetStore.TestAutomation.Api;
using WhereIsMyAi.RestSharpBase.Core.Tms;

namespace WhereIsMyAi.PetStore.TestAutomation.Hooks;

/// <summary>Skips a scenario tagged @bug:&lt;id&gt; while that bug is Open (not when also @intermittent); the decision is Core's <see cref="BugGate"/>.</summary>
[Binding]
public class BugHooks(ScenarioContext scenarioContext, IUnitTestRuntimeProvider testRuntime)
{
    // Order 0: decide before any other BeforeScenario hook does work for a scenario that will not run.
    [BeforeScenario(Order = 0)]
    public void SkipScenarioBlockedByOpenBug()
    {
        var reason = new BugGate(PetStoreConfig.BugRepositoryPath).GetSkipReason(scenarioContext.ScenarioInfo.CombinedTags);
        if (reason != null)
            testRuntime.TestIgnore(reason);
    }
}
