Data/ApplicationUserExtensions.cs
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data
{
    public static class ApplicationUserExtensions
    {
        /// <summary>
        /// Filters an IQueryable of ApplicationUser to only those that are players.
        /// </summary>
        public static IQueryable<ApplicationUser> Players(this IQueryable<ApplicationUser> users)
        {
            return users.Where(u => u.IsPlayer);
        }

        /// <summary>
        /// Filters a DbSet of ApplicationUser to only those that are players.
        /// </summary>
        public static IQueryable<ApplicationUser> Players(this DbSet<ApplicationUser> users)
        {
            return users.Where(u => u.IsPlayer);
        }
    }
}