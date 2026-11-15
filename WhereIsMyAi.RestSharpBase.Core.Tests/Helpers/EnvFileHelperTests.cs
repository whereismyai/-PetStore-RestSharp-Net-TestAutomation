using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using WhereIsMyAi.RestSharpBase.Core.Helpers;

namespace WhereIsMyAi.RestSharpBase.Core.Tests.Helpers;

public class EnvFileHelperTests
{
    private string _root = null!;
    private string _key = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("envfile-").FullName;
        _key = "CORE_TESTS_" + Guid.NewGuid().ToString("N"); // unique: no clash with real environment variables
        File.WriteAllLines(Path.Combine(_root, ".env"), [
            "# comment",
            $"{_key}=from-file",
            "QUOTED = \"quoted value\"",
            "EMPTY=",
            "REPO_PATH=Bugs",
            "not a setting",
        ]);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(_key, null);
        Directory.Delete(_root, true);
    }

    [Test]
    public void Load_FindsTheEnvFileInAParentFolder()
    {
        var nested = Directory.CreateDirectory(Path.Combine(_root, "bin", "Release", "net10.0")).FullName;

        var file = EnvFileHelper.Load(nested);

        file.Path.Should().Be(_root);
        file.Values.Should().ContainKey(_key).WhoseValue.Should().Be("from-file");
    }

    [Test]
    public void Load_TrimsSpacesAndQuotes_SkipsCommentsAndLinesWithoutEquals()
    {
        var file = EnvFileHelper.Load(_root);

        file.Values["QUOTED"].Should().Be("quoted value");
        file.Values.Should().NotContainKey("# comment").And.NotContainKey("not a setting");
    }

    [Test]
    public void GetOptional_EnvironmentVariableWinsOverTheFile()
    {
        Environment.SetEnvironmentVariable(_key, "from-environment");

        EnvFileHelper.GetOptional(_key, EnvFileHelper.Load(_root)).Should().Be("from-environment");
    }

    [Test]
    public void GetOptional_FileIsUsedWhenTheEnvironmentVariableIsNotSet()
    {
        EnvFileHelper.GetOptional(_key, EnvFileHelper.Load(_root)).Should().Be("from-file");
    }

    [Test]
    public void GetOptional_EmptyOrMissingSettingIsNull()
    {
        var file = EnvFileHelper.Load(_root);

        EnvFileHelper.GetOptional("EMPTY", file).Should().BeNull();
        EnvFileHelper.GetOptional("CORE_TESTS_MISSING_" + Guid.NewGuid().ToString("N"), file).Should().BeNull();
    }

    [Test]
    public void GetOptionalPath_RelativePathIsResolvedAgainstTheEnvFileFolder()
    {
        EnvFileHelper.GetOptionalPath("REPO_PATH", EnvFileHelper.Load(_root)).Should().Be(Path.Combine(_root, "Bugs"));
    }

    [Test]
    public void Get_ThrowsAReadableErrorForAMissingRequiredSetting()
    {
        var act = () => EnvFileHelper.Get("CORE_TESTS_MISSING_" + Guid.NewGuid().ToString("N"));

        act.Should().Throw<InvalidOperationException>().WithMessage("Setting 'CORE_TESTS_MISSING_*' not found*");
    }
}
