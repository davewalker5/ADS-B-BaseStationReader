using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.TrackerHub.Models;
using BaseStationReader.TrackerHub.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public class AirportScheduleLookupServiceTest
{
    /// <summary>
    /// Verifies that storage eligibility requires the callsign and core flight/airline codes.
    /// </summary>
    [TestMethod]
    public void IdentifyStorableMappingsTest()
    {
        var complete = new FlightIATACodeMapping
        {
            FlightIATA = "BA123", Callsign = "BAW123", AirlineIATA = "BA", AirlineICAO = "BAW"
        };
        var incomplete = new FlightIATACodeMapping
        {
            FlightIATA = "BA123", Callsign = string.Empty, AirlineIATA = "BA", AirlineICAO = "BAW"
        };

        Assert.IsTrue(FlightMappingEligibility.IsEligible(complete));
        Assert.IsFalse(FlightMappingEligibility.IsEligible(incomplete));
    }

    /// <summary>
    /// Verifies that only configured schedule providers are exposed.
    /// </summary>
    [TestMethod]
    public void FilterServicesByScheduleEndpointTest()
    {
        var settings = new ExternalApiSettings
        {
            ApiServices =
            [
                new ApiService
                {
                    Service = ApiServiceType.AeroDataBox,
                    ApiEndpoints = [new ApiEndpoint { EndpointType = ApiEndpointType.Schedules }]
                },
                new ApiService
                {
                    Service = ApiServiceType.SkyLink,
                    ApiEndpoints = [new ApiEndpoint { EndpointType = ApiEndpointType.METAR }]
                }
            ]
        };

        var services = CreateService(settings).GetServices();

        Assert.HasCount(1, services);
        Assert.AreEqual(ApiServiceType.AeroDataBox, services[0]);
    }

    /// <summary>
    /// Verifies that configured start and end times are applied to today's date.
    /// </summary>
    [TestMethod]
    public void GetConfiguredDefaultRangeTest()
    {
        var service = CreateService(new ExternalApiSettings(), new ScheduleOptions
        {
            ScheduleStartTime = "08:30",
            ScheduleEndTime = "19:45"
        });

        var range = service.GetDefaultRange(new DateTime(2026, 7, 21, 15, 20, 0));

        Assert.AreEqual(new DateTime(2026, 7, 21, 8, 30, 0), range.From);
        Assert.AreEqual(new DateTime(2026, 7, 21, 19, 45, 0), range.To);
    }

    /// <summary>
    /// Verifies that an overlong configured range is constrained to 12 hours.
    /// </summary>
    [TestMethod]
    public void ConstrainConfiguredDefaultRangeTest()
    {
        var service = CreateService(new ExternalApiSettings(), new ScheduleOptions
        {
            ScheduleStartTime = "01:00",
            ScheduleEndTime = "23:00"
        });

        var range = service.GetDefaultRange(new DateTime(2026, 7, 21));

        Assert.AreEqual(TimeSpan.FromHours(12), range.To - range.From);
    }

    /// <summary>
    /// Creates a schedule service for configuration-only tests.
    /// </summary>
    private static AirportScheduleLookupService CreateService(
        ExternalApiSettings settings,
        ScheduleOptions scheduleOptions = null)
    {
        var contextFactory = new Mock<IDbContextFactory<BaseStationReaderDbContext>>();
        var logger = new Mock<ITrackerLogger>();
        return new AirportScheduleLookupService(
            settings,
            Options.Create(scheduleOptions ?? new ScheduleOptions()),
            contextFactory.Object,
            logger.Object);
    }
}
