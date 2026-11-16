using System.IO;
using FluentAssertions;
using NUnit.Framework;
using WhereIsMyAi.RestSharpBase.Core.Tms;
using WhereIsMyAi.RestSharpBase.Core.UnitTests.Logger;

namespace WhereIsMyAi.RestSharpBase.Core.UnitTests.Tms;

public class BugGateTests
{
    private string _path = null!;
    private ListLogger _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _path = Directory.CreateTempSubdirectory("bugs-").FullName;
        _logger = new ListLogger();
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
    public void FixedBug_Runs() =>
        new BugGate(_path).GetSkipReason(["bug:BUG-002"]).Should().BeNull();

    [Test]
    public void OneOpenBugAmongSeveral_Skips() =>
        new BugGate(_path).GetSkipReason(["bug:BUG-002", "bug:BUG-001"]).Should().StartWith("Blocked by open bug BUG-001");

    [Test]
    public void NoBugTag_Runs() =>
        new BugGate(_path).GetSkipReason(["smoke", "shared-ids"]).Should().BeNull();

    [Test]
    public void InvalidStatus_IsNullAndRunsWithAWarning()
    {
        new BugRepository(_path).Find("BUG-004")!.Status.Should().BeNull();
        new BugGate(_path, _logger).GetSkipReason(["bug:BUG-004"]).Should().BeNull();
        _logger.Messages.Should().ContainSingle(message => message.StartsWith("Warning: Bug BUG-004 has no valid Status"));
    }

    [Test]
    public void UnknownBugId_RunsWithAWarning()
    {
        new BugGate(_path, _logger).GetSkipReason(["bug:BUG-999"]).Should().BeNull();
        _logger.Messages.Should().ContainSingle(message => message.StartsWith("Warning: Bug BUG-999 not found"));
    }

    [Test]
    public void RepositoryNotConfigured_RunsWithAWarning()
    {
        new BugGate(null, _logger).GetSkipReason(["bug:BUG-001"]).Should().BeNull();
        _logger.Messages.Should().ContainSingle(message => message.StartsWith("Warning: Bug repository path is unavailable (not configured)"));
    }

    [Test]
    public void RepositoryFolderMissing_RunsWithAWarning()
    {
        new BugGate(Path.Combine(_path, "missing"), _logger).GetSkipReason(["bug:BUG-001"]).Should().BeNull();
        _logger.Messages.Should().ContainSingle(message => message.StartsWith("Warning: Bug repository path is unavailable"));
    }
}
