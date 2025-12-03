using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data
{
    /// <summary>
    /// Query extensions for Pool-related IQueryable operations.
    /// </summary>
    public static class PoolExtensions
    {
        /// <summary>
        /// Returns the latest pool year from the provided queryable.
        /// Returns 0 when the sequence is empty.
        /// </summary>
        public static Pool GetLatestPoolYear(this IQueryable<Pool> pools)
        {
            if (pools is null) throw new ArgumentNullException(nameof(pools));

            return pools
                .OrderByDescending(p => p.Year)
                .FirstOrDefault();
        }

        /// <summary>
        /// Asynchronously returns the latest pool year from the provided queryable.
        /// Returns 0 when the sequence is empty.
        /// </summary>
        public static async Task<Pool> GetLatestPoolYearAsync(this IQueryable<Pool> pools, CancellationToken cancellationToken = default)
        {
            if (pools is null) throw new ArgumentNullException(nameof(pools));

            return await pools
                .OrderByDescending(p => p.Year)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}