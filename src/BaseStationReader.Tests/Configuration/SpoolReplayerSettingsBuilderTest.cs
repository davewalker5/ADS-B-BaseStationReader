using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Config;

namespace BaseStationReader.Tests.Configuration;

[TestClass]
public sealed class SpoolReplayerSettingsBuilderTest
{
    /// <summary>
    /// Verifies the utility reads its application settings from JSON.
    /// </summary>
    [TestMethod]
    public void DefaultSettingsAreReadTest()
    {
        ICommandLineParser parser = new SpoolReplayerCommandLineParser(null);
        parser.Parse([]);

        var settings = new SpoolReplayerSettingsBuilder().BuildSettings(
            parser,
            "spoolreplayersettings.json");

        Assert.AreEqual("Replay-Test.log", settings.LogFile);
        Assert.AreEqual(Severity.Warning, settings.MinimumLogLevel);
        Assert.AreEqual("test-spool", settings.SpoolFolder);
        Assert.AreEqual(12345, settings.TimeToLock);
    }

    /// <summary>
    /// Verifies supported command-line values override file settings.
    /// </summary>
    [TestMethod]
    public void CommandLineOverridesSettingsTest()
    {
        ICommandLineParser parser = new SpoolReplayerCommandLineParser(null);
        parser.Parse([
            "--log-file", "override.log",
            "--log-level", "Debug",
            "--spool-folder", "other-spool",
            "--verbose", "true"
        ]);

        var settings = new SpoolReplayerSettingsBuilder().BuildSettings(
            parser,
            "spoolreplayersettings.json");

        Assert.AreEqual("override.log", settings.LogFile);
        Assert.AreEqual(Severity.Debug, settings.MinimumLogLevel);
        Assert.AreEqual("other-spool", settings.SpoolFolder);
        Assert.IsTrue(settings.VerboseLogging);
    }

    /// <summary>
    /// Verifies both long and short utility-specific spool options are recognised.
    /// </summary>
    [TestMethod]
    public void ShortSpoolFolderOptionIsRecognisedTest()
    {
        ICommandLineParser parser = new SpoolReplayerCommandLineParser(null);
        parser.Parse(["-sf", "short-spool"]);

        Assert.AreEqual("short-spool", parser.GetValues(CommandLineOptionType.SpoolFolder)?[0]);
    }
}
