using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class SystemSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }
}
