using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    [ExcludeFromCodeCoverage]
    internal class DataCleaner : IDataCleaner
    {
        private readonly BaseStationReaderDbContext _context;

        public DataCleaner(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Clean airlines
        /// </summary>
        /// <returns></returns>
        public async Task CleanAirlines()
        {
            // Iterate over each airline
            foreach (var airline in _context.Airlines)
            {
                // Convert the properties to a standardised form
                airline.IATA = StringCleaner.CleanIATA(airline.IATA);
                airline.ICAO = StringCleaner.CleanIATA(airline.ICAO);
                airline.Name = StringCleaner.CleanName(airline.Name);
            }

            // Save changes
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Clean manufacturers
        /// </summary>
        /// <returns></returns>
        public async Task CleanManufacturers()
        {
            // Iterate over each manufacturer
            foreach (var manufacturer in _context.Manufacturers)
            {
                // Convert the properties to a standardised form
                manufacturer.Name = StringCleaner.CleanName(manufacturer.Name);
            }

            // Save changes
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Clean airlines
        /// </summary>
        /// <returns></returns>
        public async Task CleanModels()
        {
            // Iterate over each airline
            foreach (var model in _context.Models)
            {
                // Convert the properties to a standardised form
                model.IATA = StringCleaner.CleanIATA(model.IATA);
                model.ICAO = StringCleaner.CleanIATA(model.ICAO);
            }

            // Save changes
            await _context.SaveChangesAsync();
        }
    }
}