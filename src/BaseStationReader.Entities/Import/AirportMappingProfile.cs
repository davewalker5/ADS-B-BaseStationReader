using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Api;
using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Import
{
    [ExcludeFromCodeCoverage]
    public sealed class AirportMappingProfile : ClassMap<Airport>
    {
        /// <summary>
        /// Configure the mapping between airport CSV columns and airport properties.
        /// </summary>
        public AirportMappingProfile()
        {
            Map(m => m.IATA).Name("IATA");
            Map(m => m.ICAO).Name("ICAO");
            Map(m => m.Latitude).Name("Latitude");
            Map(m => m.Longitude).Name("Longitude");
            Map(m => m.ProvenanceRef).Name("Provenance");
            Map(m => m.Name).Name("Name");
        }
    }
}
