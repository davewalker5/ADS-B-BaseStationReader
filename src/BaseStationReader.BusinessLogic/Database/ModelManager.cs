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
                .Include(x => x.Provenance)
                .ToListAsync();

        /// <summary>
        /// Add a new model to the database
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public async Task<Model> AddAsync(string iata, string icao, string name, int manufacturerId, int provenanceId = 0)
        {
            // Clean the inputs so they're in a standardised format
            var cleanIATA = string.IsNullOrWhiteSpace(iata) ? null : StringCleaner.CleanIATA(iata);
            var cleanICAO = string.IsNullOrWhiteSpace(icao) ? null : StringCleaner.CleanICAO(icao);

            if (provenanceId == 0)
            {
                var local = await _context.Provenance.FirstOrDefaultAsync(x => x.SourceRef == "MANUAL");
                if (local == null)
                {
                    local = new Provenance
                    {
                        SourceRef = "MANUAL", Source = "N/A", SourceUrl = "N/A",
                        SourceDataset = "N/A", SourceVersion = "N/A", Licence = "N/A"
                    };
                    await _context.Provenance.AddAsync(local);
                    await _context.SaveChangesAsync();
                }
                provenanceId = local.Id;
            }
            else if (!await _context.Provenance.AnyAsync(x => x.Id == provenanceId))
            {
                throw new InvalidOperationException($"Provenance record {provenanceId} does not exist.");
            }
            if (!await _context.Manufacturers.AnyAsync(x => x.Id == manufacturerId))
            {
                throw new InvalidOperationException($"Manufacturer record {manufacturerId} does not exist.");
            }

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
                    ManufacturerId = manufacturerId,
                    ProvenanceId = provenanceId
                };

                await _context.Models.AddAsync(model);
                await _context.SaveChangesAsync();
            }

            return model;
        }

        /// <inheritdoc />
        public async Task<Model> UpdateAsync(
            int id,
            string iata,
            string icao,
            string name,
            int manufacturerId,
            int provenanceId)
        {
            var model = await _context.Models.FindAsync(id)
                ?? throw new InvalidOperationException($"Model record {id} does not exist.");
            if (!await _context.Manufacturers.AnyAsync(x => x.Id == manufacturerId))
            {
                throw new InvalidOperationException($"Manufacturer record {manufacturerId} does not exist.");
            }
            if (!await _context.Provenance.AnyAsync(x => x.Id == provenanceId))
            {
                throw new InvalidOperationException($"Provenance record {provenanceId} does not exist.");
            }

            var cleanIATA = string.IsNullOrWhiteSpace(iata) ? null : StringCleaner.CleanIATA(iata);
            var cleanICAO = string.IsNullOrWhiteSpace(icao) ? null : StringCleaner.CleanICAO(icao);
            var cleanName = name?.Trim() ?? "";
            if (cleanICAO is not null &&
                await _context.Models.AnyAsync(x => x.Id != id && x.ICAO == cleanICAO))
            {
                throw new InvalidOperationException($"An aircraft model with ICAO code '{cleanICAO}' already exists.");
            }
            if (cleanIATA is not null &&
                await _context.Models.AnyAsync(x => x.Id != id && x.IATA == cleanIATA))
            {
                throw new InvalidOperationException($"An aircraft model with IATA code '{cleanIATA}' already exists.");
            }
            if (await _context.Models.AnyAsync(x =>
                    x.Id != id && x.ManufacturerId == manufacturerId && x.Name == cleanName))
            {
                throw new InvalidOperationException(
                    $"A model named '{cleanName}' already exists for the selected manufacturer.");
            }

            model.IATA = cleanIATA;
            model.ICAO = cleanICAO;
            model.Name = cleanName;
            model.ManufacturerId = manufacturerId;
            model.ProvenanceId = provenanceId;
            await _context.SaveChangesAsync();
            return await GetAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(int id)
        {
            var model = await _context.Models.FindAsync(id)
                ?? throw new InvalidOperationException($"Model record {id} does not exist.");
            _context.Models.Remove(model);
            await _context.SaveChangesAsync();
        }
    }
}
