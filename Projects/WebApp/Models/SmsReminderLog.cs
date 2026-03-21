using System;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class SmsReminderLog
    {
        [Key]
        public int Id { get; set; }
        
        public int RaceId { get; set; }
        public Race Race { get; set; } = null!;
        
        public DateTime SentDate { get; set; }
        
        public string Message { get; set; } = string.Empty;
    }
}
