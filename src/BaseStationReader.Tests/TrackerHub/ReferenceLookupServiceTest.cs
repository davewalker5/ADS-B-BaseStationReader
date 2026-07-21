using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.TrackerHub.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public class ReferenceLookupServiceTest
{
    /// <summary>
    /// Verifies that each selector exposes only services with its matching endpoint type.
    /// </summary>
    [TestMethod]
    public void FilterServicesByEndpointTest()
    {
        var settings = new ExternalApiSettings
        {
            ApiServices =
            [
                new ApiService
                {
                    Service = ApiServiceType.AeroDataBox,
                    ApiEndpoints =
                    [
                        new ApiEndpoint { EndpointType = ApiEndpointType.Aircraft },
                        new ApiEndpoint { EndpointType = ApiEndpointType.Flights }
                    ]
                },
                new ApiService
                {
                    Service = ApiServiceType.AirLabs,
                    ApiEndpoints = [new ApiEndpoint { EndpointType = ApiEndpointType.Flights }]
                },
                new ApiService
                {
                    Service = ApiServiceType.SkyLink,
                    ApiEndpoints = [new ApiEndpoint { EndpointType = ApiEndpointType.Aircraft }]
                }
            ]
        };
        var service = CreateService(settings);

        var aircraftServices = service.GetServices(ApiEndpointType.Aircraft);
        var flightServices = service.GetServices(ApiEndpointType.Flights);

        CollectionAssert.AreEquivalent(
            new[] { ApiServiceType.AeroDataBox, ApiServiceType.SkyLink },
            aircraftServices.ToArray());
        CollectionAssert.AreEquivalent(
            new[] { ApiServiceType.AeroDataBox, ApiServiceType.AirLabs },
            flightServices.ToArray());
    }

    /// <summary>
    /// Verifies that unrelated endpoint types are not exposed by the lookup service.
    /// </summary>
    [TestMethod]
    public void RejectUnsupportedEndpointTest()
    {
        var service = CreateService(new ExternalApiSettings());
        Assert.IsEmpty(service.GetServices(ApiEndpointType.METAR));
    }

    /// <summary>
    /// Creates a reference lookup service for configuration-only tests.
    /// </summary>
    /// <param name="settings">The settings to expose to the service.</param>
    /// <returns>The configured service.</returns>
    private static ReferenceLookupService CreateService(ExternalApiSettings settings)
    {
        var contextFactory = new Mock<IDbContextFactory<BaseStationReaderDbContext>>();
        var logger = new Mock<ITrackerLogger>();
        return new ReferenceLookupService(settings, contextFactory.Object, logger.Object);
    }
}
