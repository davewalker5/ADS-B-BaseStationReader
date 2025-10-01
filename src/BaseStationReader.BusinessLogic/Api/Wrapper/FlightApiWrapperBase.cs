using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Api.Wrapper
{
    internal abstract class FlightApiWrapperBase
    {
        protected readonly IAirlineManager _airlineManager;
        protected readonly IFlightManager _flightManager;

        public FlightApiWrapperBase(IAirlineManager airlineManager, IFlightManager flightManager)
        {
            _airlineManager = airlineManager;
            _flightManager = flightManager;
        }

        /// <summary>
        /// Given a set of API property values representing a flight, create and save a new flight
        /// locally
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected async Task<Flight> SaveFlight(Dictionary<ApiProperty, string> properties)
        {
            // Save the airline and the flight
            var airline = await _airlineManager.AddAsync(
                properties[ApiProperty.AirlineIATA], properties[ApiProperty.AirlineICAO], properties[ApiProperty.AirlineName]);

            Flight flight = await _flightManager.AddAsync(
                properties[ApiProperty.FlightIATA],
                properties[ApiProperty.FlightICAO],
                properties[ApiProperty.FlightNumber],
                properties[ApiProperty.EmbarkationIATA],
                properties[ApiProperty.DestinationIATA],
                airline.Id);

            // There may be additional aircraft details in the flight properties
            flight.AircraftAddress = properties[ApiProperty.AircraftAddress];
            flight.ModelICAO = properties[ApiProperty.ModelICAO];

            // And as we now have a matching flight, return it
            return flight;
        }
    }
}