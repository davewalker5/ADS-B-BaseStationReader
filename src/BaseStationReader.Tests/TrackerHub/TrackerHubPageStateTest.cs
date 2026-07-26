using BaseStationReader.Entities.Config;
using BaseStationReader.TrackerHub.Models;
using BaseStationReader.TrackerHub.Services;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public class TrackerHubPageStateTest
{
    /// <summary>
    /// Verifies that one UI-session state object retains each tab's submitted criteria.
    /// </summary>
    [TestMethod]
    public void RetainPageCriteriaInMemoryTest()
    {
        var state = new TrackerHubPageState
        {
            Lookup = new LookupPageState(
                ApiServiceType.AeroDataBox, ApiServiceType.AirLabs, "406A3D", "BAW123"),
            Schedule = new SchedulePageState(
                ApiServiceType.AeroDataBox, "LHR",
                new DateTime(2026, 7, 26, 9, 0, 0),
                new DateTime(2026, 7, 26, 21, 0, 0)),
            Weather = new WeatherPageState(
                ApiEndpointType.METAR, ApiServiceType.CheckWXApi, "EGLL")
        };

        Assert.AreEqual("406A3D", state.Lookup.Address);
        Assert.AreEqual("LHR", state.Schedule.Iata);
        Assert.AreEqual("EGLL", state.Weather.Icao);
    }

    /// <summary>
    /// Verifies that separate scoped-state instances do not share retained inputs.
    /// </summary>
    [TestMethod]
    public void KeepUiSessionsIsolatedTest()
    {
        var firstSession = new TrackerHubPageState
        {
            Lookup = new LookupPageState(
                ApiServiceType.AeroDataBox, ApiServiceType.AeroDataBox, "406A3D", "BAW123")
        };
        var secondSession = new TrackerHubPageState();

        Assert.IsNotNull(firstSession.Lookup);
        Assert.IsNull(secondSession.Lookup);
    }
}
