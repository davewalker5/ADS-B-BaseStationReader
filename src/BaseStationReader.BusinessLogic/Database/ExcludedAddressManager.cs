#nullable enable

using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal sealed class ExcludedAddressManager : IExcludedAddressManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ExcludedAddressManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ExcludedAddress>> SearchAsync(
            string? address,
            CancellationToken cancellationToken = default)
        {
            var normalisedAddress = address?.Trim().ToUpperInvariant() ?? string.Empty;
            IQueryable<ExcludedAddress> query = _context.ExcludedAddresses.AsNoTracking();
            if (normalisedAddress.Length > 0)
            {
                query = query.Where(exclusion => exclusion.Address.Contains(normalisedAddress));
            }

            return await query
                .OrderBy(exclusion => exclusion.Address)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Return true if an aircraft address is excluded
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<bool> IsExcludedAsync(string address)
        {
            var exclusions = await ListAsync(x => x.Address == address);
            return exclusions.Count > 0;
        }

        /// <summary>
        /// List all exclusions matching the specified predicate
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<ExcludedAddress>> ListAsync(Expression<Func<ExcludedAddress, bool>> predicate)
            => await _context.ExcludedAddresses
                .Where(predicate)
                .OrderBy(x => x.Address)
                .ToListAsync();

        /// <summary>
        /// Adds an aircraft address exclusion if it does not already exist.
        /// </summary>
        /// <param name="address">The ICAO aircraft address to exclude.</param>
        /// <param name="cancellationToken">A token used to cancel the database operation.</param>
        /// <returns>The existing or newly created exclusion.</returns>
        public async Task<ExcludedAddress> AddAsync(
            string address,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(address);
            var normalisedAddress = address.Trim().ToUpperInvariant();

            // Check there's not already an exclusion for this address
            var exclusion = await _context.ExcludedAddresses
                .FirstOrDefaultAsync(x => x.Address == normalisedAddress, cancellationToken);
            if (exclusion == null)
            {
                // Create a new exclusion
                exclusion = new ExcludedAddress()
                {
                    Address = normalisedAddress
                };

                // Save the aircraft
                await _context.ExcludedAddresses.AddAsync(exclusion, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return exclusion;
        }
        
        /// <summary>
        /// Delete the exclusion record for the specified address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string address, CancellationToken cancellationToken = default)
        {
            // Find the exclusion record
            var exclusion = await _context.ExcludedAddresses
                .FirstOrDefaultAsync(x => x.Address == address, cancellationToken);
            if (exclusion != null)
            {
                // Found one, so remove it
                _context.Remove(exclusion);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
