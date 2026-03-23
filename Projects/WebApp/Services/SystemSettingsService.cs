using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services
{
    public interface ISystemSettingsService
    {
        Task<string?> GetSettingAsync(string key);
        Task<bool> GetBoolSettingAsync(string key, bool defaultValue = false);
        Task SetSettingAsync(string key, string value, string? description = null);
        Task<Dictionary<string, string>> GetAllSettingsAsync();
    }

    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly ApplicationDbContext _context;

        public SystemSettingsService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value;
        }

        public async Task<bool> GetBoolSettingAsync(string key, bool defaultValue = false)
        {
            var value = await GetSettingAsync(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        public async Task SetSettingAsync(string key, string value, string? description = null)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                setting = new SystemSettings
                {
                    Key = key,
                    Value = value,
                    Description = description,
                    LastModified = DateTime.UtcNow
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.LastModified = DateTime.UtcNow;
                if (description != null)
                {
                    setting.Description = description;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<string, string>> GetAllSettingsAsync()
        {
            var settings = await _context.SystemSettings.ToListAsync();
            return settings.ToDictionary(s => s.Key, s => s.Value);
        }
    }
}
