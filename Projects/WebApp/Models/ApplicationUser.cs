using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client;
using System.Collections.Generic;

namespace WebApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsPlayer { get; set; } = true;
        public int? PrimaryDriverFirstHalfId { get; set; }
        public int? PrimaryDriverSecondHalfId { get; set; }
        public Driver PrimaryDriverFirstHalf { get; set; } = default!;
        public Driver PrimaryDriverSecondHalf { get; set; } = default!;

		public ICollection<Pick> Picks { get; set; } = new List<Pick>();
        public ICollection<Pool> Pools { get; set; } = new List<Pool>();
    }
}