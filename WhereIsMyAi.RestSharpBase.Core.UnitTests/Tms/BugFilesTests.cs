using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using WhereIsMyAi.RestSharpBase.Core.Tms;

namespace WhereIsMyAi.RestSharpBase.Core.UnitTests.Tms;

/// <summary>Checks this repository's Bugs/*.md, copied next to the tests by the .csproj, so a bad Status fails the required build.</summary>
public class BugFilesTests
{
    private static readonly string BugsPath = Path.Combine(AppContext.BaseDirectory, "Bugs");

    [Test]
    public void EveryBugFile_HasAValidStatus()
    {
        var repository = new BugRepository(BugsPath);
        var statuses = Directory.GetFiles(BugsPath, "*.md")
            .Select(file => Path.GetFileNameWithoutExtension(file))
            .ToDictionary(id => id, id => repository.Find(id)?.Status);

        statuses.Should().NotBeEmpty()
            .And.NotContainValue(null, "every bug needs 'Status: {0}'", string.Join(" | ", Enum.GetNames<BugStatus>()));
    }
}
