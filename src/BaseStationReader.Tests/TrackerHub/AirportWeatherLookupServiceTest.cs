using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.TrackerHub.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public class AirportWeatherLookupServiceTest
{
    /// <summary>
    /// Verifies that configured services are filtered by the selected weather endpoint.
    /// </summary>
    [TestMethod]
    public void FilterServicesByWeatherEndpointTest()
    {
        // Include one METAR-only service and one service supporting both report types.
        var settings = new ExternalApiSettings
        {
            ApiServices =
            [
                new ApiService
                {
                    Service = ApiServiceType.CheckWXApi,
                    ApiEndpoints = [new ApiEndpoint { EndpointType = ApiEndpointType.METAR }]
                },
                new ApiService
                {
                    Service = ApiServiceType.SkyLink,
                    ApiEndpoints =
                    [
                        new ApiEndpoint { EndpointType = ApiEndpointType.METAR },
                        new ApiEndpoint { EndpointType = ApiEndpointType.TAF }
                    ]
                },
                new ApiService
                {
                    Service = ApiServiceType.AirLabs,
                    ApiEndpoints = [new ApiEndpoint { EndpointType = ApiEndpointType.Flights }]
                }
            ]
        };
        var service = CreateService(settings);

        var metarServices = service.GetServices(ApiEndpointType.METAR);
        var tafServices = service.GetServices(ApiEndpointType.TAF);

        Assert.HasCount(2, metarServices);
        Assert.Contains(ApiServiceType.CheckWXApi, metarServices);
        Assert.Contains(ApiServiceType.SkyLink, metarServices);
        Assert.HasCount(1, tafServices);
        Assert.AreEqual(ApiServiceType.SkyLink, tafServices[0]);
    }

    /// <summary>
    /// Verifies that unsupported endpoint types do not expose API services.
    /// </summary>
    [TestMethod]
    public void RejectNonWeatherEndpointTest()
    {
        // Weather service selection must never include unrelated configured endpoints.
        var service = CreateService(new ExternalApiSettings());

        Assert.IsEmpty(service.GetServices(ApiEndpointType.Flights));
    }

    /// <summary>
    /// Creates a service suitable for configuration-only tests.
    /// </summary>
    /// <param name="settings">The API configuration under test.</param>
    /// <returns>The configured airport weather lookup service.</returns>
    private static AirportWeatherLookupService CreateService(ExternalApiSettings settings)
    {
        // These tests exercise filtering before either dependency is used for a remote lookup.
        var contextFactory = new Mock<IDbContextFactory<BaseStationReaderDbContext>>();
        var logger = new Mock<ITrackerLogger>();
        return new AirportWeatherLookupService(settings, contextFactory.Object, logger.Object);
    }
}
