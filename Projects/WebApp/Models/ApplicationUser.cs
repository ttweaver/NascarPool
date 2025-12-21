using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsPlayer { get; set; } = true;

        // Not mapped properties used for binding & validation on the Create page.
        // These are not persisted to the database; PasswordHash will be stored instead.
        [NotMapped]
        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [NotMapped]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

		public ICollection<Pick> Picks { get; set; } = new List<Pick>();
        public ICollection<Pool> Pools { get; set; } = new List<Pool>();
        
        // Add collection for pool-specific primary drivers
        public ICollection<UserPoolPrimaryDriver> PoolPrimaryDrivers { get; set; } = new List<UserPoolPrimaryDriver>();
    }
}