#nullable enable

using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal sealed class ExcludedCallsignManager : IExcludedCallsignManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ExcludedCallsignManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ExcludedCallsign>> SearchAsync(
            string? callsign,
            CancellationToken cancellationToken = default)
        {
            var normalisedCallsign = callsign?.Trim().ToUpperInvariant() ?? string.Empty;
            IQueryable<ExcludedCallsign> query = _context.ExcludedCallsigns.AsNoTracking();
            if (normalisedCallsign.Length > 0)
            {
                query = query.Where(exclusion => exclusion.Callsign.Contains(normalisedCallsign));
            }

            return await query
                .OrderBy(exclusion => exclusion.Callsign)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Return true if a callsign is excluded
        /// </summary>
        /// <param name="callsign"></param>
        /// <returns></returns>
        public async Task<bool> IsExcludedAsync(string callsign)
        {
            var exclusions = await ListAsync(x => x.Callsign == callsign);
            return exclusions.Count > 0;
        }

        /// <summary>
        /// List all exclusions matching the specified predicate
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<ExcludedCallsign>> ListAsync(Expression<Func<ExcludedCallsign, bool>> predicate)
            => await _context.ExcludedCallsigns
                .Where(predicate)
                .OrderBy(x => x.Callsign)
                .ToListAsync();

        /// <summary>
        /// Adds a flight callsign exclusion if it does not already exist.
        /// </summary>
        /// <param name="callsign">The flight callsign to exclude.</param>
        /// <param name="cancellationToken">A token used to cancel the database operation.</param>
        /// <returns>The existing or newly created exclusion.</returns>
        public async Task<ExcludedCallsign> AddAsync(
            string callsign,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(callsign);
            var normalisedCallsign = callsign.Trim().ToUpperInvariant();

            // Check there's not already an exclusion for this callsign
            var exclusion = await _context.ExcludedCallsigns
                .FirstOrDefaultAsync(x => x.Callsign == normalisedCallsign, cancellationToken);
            if (exclusion == null)
            {
                // Create a new exclusion
                exclusion = new ExcludedCallsign()
                {
                    Callsign = normalisedCallsign
                };

                // Save the aircraft
                await _context.ExcludedCallsigns.AddAsync(exclusion, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return exclusion;
        }
        
        /// <summary>
        /// Delete the exclusion record for the specified callsign
        /// </summary>
        /// <param name="callsign"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string callsign, CancellationToken cancellationToken = default)
        {
            // Find the exclusion record
            var exclusion = await _context.ExcludedCallsigns
                .FirstOrDefaultAsync(x => x.Callsign == callsign, cancellationToken);
            if (exclusion != null)
            {
                // Found one, so remove it
                _context.Remove(exclusion);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
