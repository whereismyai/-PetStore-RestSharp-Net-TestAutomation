using System.IO;
using FluentAssertions;
using NUnit.Framework;
using WhereIsMyAi.RestSharpBase.Core.Tms;

namespace WhereIsMyAi.RestSharpBase.Core.UnitTests.Tms;

public class BugGateTests
{
    private string _path = null!;

    [SetUp]
    public void SetUp()
    {
        _path = Directory.CreateTempSubdirectory("bugs-").FullName;
        File.WriteAllText(Path.Combine(_path, "BUG-001.md"), "# BUG-001\n- Id: BUG-001\n- Status: Open\n- Title: Pet is overwritten\n");
        File.WriteAllText(Path.Combine(_path, "BUG-002.md"), "Id: BUG-002\nStatus: Fixed\nTitle: Ids repeat\n");
        File.WriteAllText(Path.Combine(_path, "BUG-003.md"), "- id: BUG-003\n- status: open\n- title: lower-case keys\n");
        File.WriteAllText(Path.Combine(_path, "BUG-004.md"), "- Id: BUG-004\n- Status: Opne\n- Title: typo in the status\n");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_path, true);

    [Test]
    public void OpenBug_SkipsWithItsTitle() =>
        new BugGate(_path).GetSkipReason(["smoke", "bug:BUG-001"]).Should().Be("Blocked by open bug BUG-001: Pet is overwritten");

    [Test]
    public void OpenBug_KeysAndStatusAreCaseInsensitive() =>
        new BugGate(_path).GetSkipReason(["BUG:BUG-003"]).Should().Be("Blocked by open bug BUG-003: lower-case keys");

    [Test]
    public void OpenBug_TaggedIntermittent_Runs() =>
        new BugGate(_path).GetSkipReason(["bug:BUG-001", "Intermittent"]).Should().BeNull();

    [Test]
    public void FixedBug_Runs() =>
        new BugGate(_path).GetSkipReason(["bug:BUG-002"]).Should().BeNull();

    [Test]
    public void OneOpenBugAmongSeveral_Skips() =>
        new BugGate(_path).GetSkipReason(["bug:BUG-002", "bug:BUG-001"]).Should().StartWith("Blocked by open bug BUG-001");

    [Test]
    public void NoBugTag_Runs() =>
        new BugGate(_path).GetSkipReason(["smoke", "shared-ids"]).Should().BeNull();

    [Test]
    public void InvalidStatus_IsNullAndRuns()
    {
        new BugRepository(_path).Find("BUG-004")!.Status.Should().BeNull();
        new BugGate(_path).GetSkipReason(["bug:BUG-004"]).Should().BeNull();
    }

    [Test]
    public void UnknownBugId_Runs() =>
        new BugGate(_path).GetSkipReason(["bug:BUG-999"]).Should().BeNull();

    [Test]
    public void RepositoryNotConfigured_Runs() =>
        new BugGate(null).GetSkipReason(["bug:BUG-001"]).Should().BeNull();

    [Test]
    public void RepositoryFolderMissing_Runs() =>
        new BugGate(Path.Combine(_path, "missing")).GetSkipReason(["bug:BUG-001"]).Should().BeNull();
}
