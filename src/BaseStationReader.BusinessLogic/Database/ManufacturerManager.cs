using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class ManufacturerManager : IManufacturerManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ManufacturerManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first manufacturer matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<Manufacturer> GetAsync(Expression<Func<Manufacturer, bool>> predicate)
        {
            List<Manufacturer> manufacturers = await ListAsync(predicate);
            return manufacturers.FirstOrDefault();
        }

        /// <summary>
        /// Return all manufacturers matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<Manufacturer>> ListAsync(Expression<Func<Manufacturer, bool>> predicate)
            => await _context.Manufacturers.Where(predicate).ToListAsync();

        /// <summary>
        /// Add a manufacturer, if it doesn't already exist
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Manufacturer> AddAsync(string name)
        {
            // Clean the inputs so they're in a standardised format
            var clean = StringCleaner.CleanName(name);

            // Look for a matching record
            var manufacturer = await GetAsync(a => a.Name == clean);

            if (manufacturer == null)
            {
                // No match, so create a new record
                manufacturer = new Manufacturer { Name = clean };

                await _context.Manufacturers.AddAsync(manufacturer);
                await _context.SaveChangesAsync();
            }

            return manufacturer;
        }
    }
}
