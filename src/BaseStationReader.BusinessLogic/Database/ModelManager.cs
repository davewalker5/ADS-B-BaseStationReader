using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class ModelManager : IModelManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ModelManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return a model by either ICAO or IATA code, whichever is specified
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Model> GetAsync(string iata, string icao, string name)
        {
            Model model = null;

            if (!string.IsNullOrEmpty(icao))
            {
                model = await GetAsync(x => x.ICAO == icao);
            }
            else if (!string.IsNullOrEmpty(iata))
            {
                model = await GetAsync(x => x.IATA == iata);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                model = await GetAsync(x => x.Name.ToLower() == name.ToLower());
            }

            return model;
        }

        /// <summary>
        /// Get the first aircraft model matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<Model> GetAsync(Expression<Func<Model, bool>> predicate)
        {
            List<Model> models = await ListAsync(predicate);
            return models.FirstOrDefault();
        }

        /// <summary>
        /// Return all aircraft models matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<Model>> ListAsync(Expression<Func<Model, bool>> predicate)
            => await _context
                .Models
                .Where(predicate)
                .Include(x => x.Manufacturer)
                .ToListAsync();

        /// <summary>
        /// Add a new model to the database
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public async Task<Model> AddAsync(string iata, string icao, string name, int manufacturerId)
        {
            // Clean the inputs so they're in a standardised format
            var cleanIATA = StringCleaner.CleanIATA(iata);
            var cleanICAO = StringCleaner.CleanICAO(icao);

            // Look for a matching record
            var model = await GetAsync(cleanIATA, cleanICAO, name);

            if (model == null)
            {
                // No match, so create a new record
                model = new Model
                {
                    IATA = cleanIATA,
                    ICAO = cleanICAO,
                    Name = name,
                    ManufacturerId = manufacturerId
                };

                await _context.Models.AddAsync(model);
                await _context.SaveChangesAsync();
            }

            return model;
        }
    }
}
