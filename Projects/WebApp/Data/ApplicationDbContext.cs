using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Pool> Pools { get; set; }
        public DbSet<Race> Races { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Pick> Picks { get; set; }
        public DbSet<RaceResult> RaceResults { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<AllowedUsers> AllowedUsers { get; set; }
        public DbSet<UserPoolPrimaryDriver> UserPoolPrimaryDrivers { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships and constraints as needed
            // Example: builder.Entity<Pool>().HasMany(p => p.Members).WithMany(u => u.Pools);

            // Configure UserPoolPrimaryDriver relationships
            //builder.Entity<UserPoolPrimaryDriver>()
            //    .HasOne(uppd => uppd.User)
            //    .WithMany(u => u.PoolPrimaryDrivers)
            //    .HasForeignKey(uppd => uppd.UserId)
            //    .OnDelete(DeleteBehavior.Cascade);
            
            //builder.Entity<UserPoolPrimaryDriver>()
            //    .HasOne(uppd => uppd.Pool)
            //    .WithMany(p => p.UserPrimaryDrivers)
            //    .HasForeignKey(uppd => uppd.PoolId)
            //    .OnDelete(DeleteBehavior.Cascade);
            
            //builder.Entity<UserPoolPrimaryDriver>()
            //    .HasOne(uppd => uppd.PrimaryDriverFirstHalf)
            //    .WithMany()
            //    .HasForeignKey(uppd => uppd.PrimaryDriverFirstHalfId)
            //    .OnDelete(DeleteBehavior.Restrict);
            
            //builder.Entity<UserPoolPrimaryDriver>()
            //    .HasOne(uppd => uppd.PrimaryDriverSecondHalf)
            //    .WithMany()
            //    .HasForeignKey(uppd => uppd.PrimaryDriverSecondHalfId)
            //    .OnDelete(DeleteBehavior.Restrict);
            
            //// Add unique constraint to ensure one record per user per pool
            //builder.Entity<UserPoolPrimaryDriver>()
            //    .HasIndex(uppd => new { uppd.UserId, uppd.PoolId })
            //    .IsUnique();
        }
    }
}
