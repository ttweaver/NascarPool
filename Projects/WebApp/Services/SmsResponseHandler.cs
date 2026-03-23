// SMS Auto-Response Handler
// This class should be implemented to handle STOP and HELP keyword responses

using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WebApp.Models;
using Microsoft.AspNetCore.Identity;

namespace WebApp.Services
{
    public interface ISmsResponseHandler
    {
        Task HandleIncomingSms(string fromPhoneNumber, string messageBody);
    }

    public class SmsResponseHandler : ISmsResponseHandler
    {
        private readonly ISmsService _smsService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SmsResponseHandler> _logger;

        public SmsResponseHandler(
            ISmsService smsService,
            UserManager<ApplicationUser> userManager,
            ILogger<SmsResponseHandler> logger)
        {
            _smsService = smsService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task HandleIncomingSms(string fromPhoneNumber, string messageBody)
        {
            if (string.IsNullOrWhiteSpace(messageBody))
                return;

            var normalizedMessage = messageBody.Trim().ToUpperInvariant();

            try
            {
                switch (normalizedMessage)
                {
                    case "STOP":
                    case "STOPALL":
                    case "UNSUBSCRIBE":
                    case "CANCEL":
                    case "END":
                    case "QUIT":
                        await HandleStopRequest(fromPhoneNumber);
                        break;

                    case "HELP":
                    case "INFO":
                        await HandleHelpRequest(fromPhoneNumber);
                        break;

                    case "START":
                    case "UNSTOP":
                        await HandleStartRequest(fromPhoneNumber);
                        break;

                    default:
                        // Log unrecognized message but don't respond
                        _logger.LogInformation("Unrecognized SMS message from {PhoneNumber}: {Message}", 
                            fromPhoneNumber, messageBody);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling incoming SMS from {PhoneNumber}", fromPhoneNumber);
            }
        }

        private async Task HandleStopRequest(string phoneNumber)
        {
            _logger.LogInformation("STOP request received from {PhoneNumber}", phoneNumber);

            // Find user by phone number and opt them out
            var users = _userManager.Users.Where(u => u.PhoneNumber == phoneNumber).ToList();
            
            foreach (var user in users)
            {
                user.SmsOptIn = false;
                user.SmsOptInDate = null;
                await _userManager.UpdateAsync(user);
                
                _logger.LogInformation("User {UserId} opted out via STOP message", user.Id);
            }

            // Send confirmation message (this is required by Twilio)
            var stopMessage = "Fast Lap Fantasy: You have been unsubscribed from SMS notifications. " +
                             "You will not receive any more messages. " +
                             "Reply START to resubscribe or visit our website to update preferences.";
            
            await _smsService.SendSmsAsync(phoneNumber, stopMessage);
        }

        private async Task HandleHelpRequest(string phoneNumber)
        {
            _logger.LogInformation("HELP request received from {PhoneNumber}", phoneNumber);

            var helpMessage = "Fast Lap Fantasy - NASCAR Fantasy Pool SMS Notifications\n\n" +
                            "We send race reminders and pool updates. Msg frequency varies. " +
                            "Msg & data rates may apply.\n\n" +
                            "Reply STOP to opt-out\n" +
                            "Reply START to opt-in\n\n" +
                            "Support: support@fastlapfantasy.com or +1-555-123-4567\n\n" +
                            "Terms: fastlapfantasy.com/TermsOfService";

            await _smsService.SendSmsAsync(phoneNumber, helpMessage);
        }

        private async Task HandleStartRequest(string phoneNumber)
        {
            _logger.LogInformation("START request received from {PhoneNumber}", phoneNumber);

            // Find user by phone number and opt them back in
            var users = _userManager.Users.Where(u => u.PhoneNumber == phoneNumber).ToList();
            
            bool userFound = false;
            foreach (var user in users)
            {
                user.SmsOptIn = true;
                user.SmsOptInDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                
                _logger.LogInformation("User {UserId} opted back in via START message", user.Id);
                userFound = true;
            }

            if (userFound)
            {
                var startMessage = "Fast Lap Fantasy: You are now subscribed to SMS notifications. " +
                                 "You will receive race reminders and pool updates. " +
                                 "Reply STOP to opt-out or HELP for assistance. Msg & data rates may apply.";
                
                await _smsService.SendSmsAsync(phoneNumber, startMessage);
            }
            else
            {
                var notFoundMessage = "Fast Lap Fantasy: Phone number not found in our system. " +
                                    "Please visit fastlapfantasy.com to create an account and opt-in to SMS notifications.";
                
                await _smsService.SendSmsAsync(phoneNumber, notFoundMessage);
            }
        }
    }
}
