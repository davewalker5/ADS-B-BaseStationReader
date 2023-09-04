using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace BaseStationReader.Logic.Database
{
    public class PositionWriter : IPositionWriter
    {
        private readonly BaseStationReaderDbContext _context;
        private readonly PropertyInfo[] _positionProperties = typeof(AircraftPosition)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.Name != "Id")
            .ToArray();

        public PositionWriter(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get the first position record matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<AircraftPosition> GetAsync(Expression<Func<AircraftPosition, bool>> predicate)
        {
            List<AircraftPosition> aircraft = await _context.AircraftPositions.Where(predicate).ToListAsync();
#pragma warning disable CS8603
            return aircraft.FirstOrDefault();
#pragma warning restore CS8603
        }

        /// <summary>
        /// List all position records matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<AircraftPosition>> ListAsync(Expression<Func<AircraftPosition, bool>> predicate)
            => await _context.AircraftPositions.Where(predicate).ToListAsync();

        /// <summary>
        /// Write an aircraft position to the database, either creating a new record or updating an existing one
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public async Task<AircraftPosition> WriteAsync(AircraftPosition template)
        {
            // If the template has an ID associated with it, retrieve that record for update
            AircraftPosition? position = null;
            if (template.Id > 0)
            {
                position = await GetAsync(x => x.Id == template.Id);
            }

            // If we still don't have an aircraft position instance, we're creating a new one
            if (position == null)
            {
                position = new AircraftPosition();
                await _context.AddAsync(position);
            }

            // Update the position properties
            foreach (var positionProperty in _positionProperties)
            {
                var updated = positionProperty.GetValue(template);
                positionProperty.SetValue(position, updated);
            }

            // Save changes
            await _context.SaveChangesAsync();

            return position;
        }
    }
}
