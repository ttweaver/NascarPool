using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace WebApp.Services
{
    public class TwilioSmsService : ISmsService
    {
        private readonly SmsSettings _settings;
        private readonly ILogger<TwilioSmsService> _logger;

        public TwilioSmsService(IOptions<SmsSettings> options, ILogger<TwilioSmsService> logger)
        {
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (_settings.EnableSms && !string.IsNullOrEmpty(_settings.AccountSid) && !string.IsNullOrEmpty(_settings.AuthToken))
            {
                TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
            }
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            if (!_settings.EnableSms)
            {
                _logger.LogInformation("SMS is disabled. Would have sent to {PhoneNumber}: {Message}", phoneNumber, message);
                return;
            }

            // NOTE: Callers of this method should verify that the user has opted in to SMS
            // notifications (ApplicationUser.SmsOptIn == true) before calling this method.
            // This service does not check opt-in status to allow for system messages like
            // STOP confirmations which must be sent regardless of opt-in status.

            try
            {
                var messageResource = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(_settings.FromPhoneNumber),
                    to: new PhoneNumber(phoneNumber)
                );

                _logger.LogInformation("SMS sent successfully. SID: {MessageSid}, To: {PhoneNumber}", 
                    messageResource.Sid, phoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
                throw;
            }
        }

        public async Task SendGroupSmsAsync(List<string> phoneNumbers, string message)
        {
            if (!_settings.EnableSms)
            {
                _logger.LogInformation("SMS is disabled. Would have sent to {Count} recipients: {Message}", 
                    phoneNumbers.Count, message);
                return;
            }

            var tasks = new List<Task>();
            foreach (var phoneNumber in phoneNumbers)
            {
                tasks.Add(SendSmsAsync(phoneNumber, message));
            }

            try
            {
                await Task.WhenAll(tasks);
                _logger.LogInformation("Group SMS sent to {Count} recipients", phoneNumbers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send group SMS to some recipients");
                throw;
            }
        }
    }
}
