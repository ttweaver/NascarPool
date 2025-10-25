using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace WebApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public ICollection<Pick> Picks { get; set; } = new List<Pick>();
        public ICollection<Pool> Pools { get; set; } = new List<Pool>();
    }
}