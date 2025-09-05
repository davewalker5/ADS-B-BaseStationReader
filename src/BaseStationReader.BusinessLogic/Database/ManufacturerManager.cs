using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BaseStationReader.BusinessLogic.Database
{
    public class ManufacturerManager : IManufacturerManager
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

#pragma warning disable CS8603
            return manufacturers.FirstOrDefault();
#pragma warning restore CS8603
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
            var manufacturer = await GetAsync(a => a.Name == name);

            if (manufacturer == null)
            {
                manufacturer = new Manufacturer { Name = name };
                await _context.Manufacturers.AddAsync(manufacturer);
                await _context.SaveChangesAsync();
            }

            return manufacturer;
        }
    }
}
