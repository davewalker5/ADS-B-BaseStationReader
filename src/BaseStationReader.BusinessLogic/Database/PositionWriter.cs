using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    public class PositionWriter : IPositionWriter
    {
        private readonly BaseStationReaderDbContext _context;
        private readonly PropertyInfo[] _positionProperties = typeof(AircraftPosition)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.Name != "Id")
            .ToArray();

        public PositionWriter(BaseStationReaderDbContext context)
            => _context = context;

        /// <summary>
        /// Get the first position record matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<AircraftPosition> GetAsync(Expression<Func<AircraftPosition, bool>> predicate)
        {
            List<AircraftPosition> aircraft = await ListAsync(predicate);
            return aircraft.FirstOrDefault();
        }

        /// <summary>
        /// List all position records matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<AircraftPosition>> ListAsync(Expression<Func<AircraftPosition, bool>> predicate)
            => await _context.Positions.Where(predicate).ToListAsync();

        /// <summary>
        /// Write an aircraft position to the database, either creating a new record or updating an existing one
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public async Task<AircraftPosition> WriteAsync(AircraftPosition template)
        {
            // Find existing matching position records
            var position = await _context.Positions.FirstOrDefaultAsync(x => x.Id == template.Id);
            if (position != null)
            {
                // Record found, so update its properties
                UpdateProperties(template, position);
            }
            else
            {
                // Existing record not found, so add a new one
                position = new();
                UpdateProperties(template, position);
                await _context.Positions.AddAsync(position);
            }

            // Save changes
            await _context.SaveChangesAsync();
            return position;
        }

        /// <summary>
        /// Update the properties of an aircraft position  
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        private void UpdateProperties(AircraftPosition source, AircraftPosition destination)
        {
            foreach (var positionProperty in _positionProperties)
            {
                var updated = positionProperty.GetValue(source);
                positionProperty.SetValue(destination, updated);
            }
        }
    }
}
