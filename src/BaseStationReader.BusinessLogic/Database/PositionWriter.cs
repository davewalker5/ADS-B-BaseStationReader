using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class PositionWriter : IPositionWriter
    {
        private readonly BaseStationReaderDbContext _context;
        private readonly PropertyInfo[] _positionProperties;

        public PositionWriter(BaseStationReaderDbContext context)
        {
            _context = context;
            // Copy database columns only; queue-only SessionId and the Aircraft navigation are deliberately
            // excluded so relationship fix-up cannot alter the selected AircraftId.
            _positionProperties = context.Model.FindEntityType(typeof(AircraftPosition))!
                .GetProperties()
                .Where(property => property.Name != nameof(AircraftPosition.Id))
                .Select(property => property.PropertyInfo)
                .Where(property => property is not null)
                .Cast<PropertyInfo>()
                .ToArray();
        }

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
