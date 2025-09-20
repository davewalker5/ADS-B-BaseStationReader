using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
            var model = await GetAsync(x => (x.IATA == iata) || (x.ICAO == icao) || (x.Name == name));
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
