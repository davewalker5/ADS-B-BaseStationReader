using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    public class ModelManager : IModelManager
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
        /// <returns></returns>
        public async Task<Model> GetByCodeAsync(string iata, string icao)
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
            var model = await GetByCodeAsync(iata, icao);
            if (model == null)
            {
                model = new Model
                {
                    IATA = iata,
                    ICAO = icao,
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
