using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace WebApp.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public EmailSender(IOptions<EmailSettings> options)
        {
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                // Choose socket options based on configuration
                SecureSocketOptions socketOptions = SecureSocketOptions.Auto;
                if (_settings.UseSsl) socketOptions = SecureSocketOptions.SslOnConnect;
                else if (_settings.UseStartTls) socketOptions = SecureSocketOptions.StartTls;

                await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(_settings.Username))
                {
                    await client.AuthenticateAsync(_settings.Username, _settings.Password).ConfigureAwait(false);
                }

                await client.SendAsync(message).ConfigureAwait(false);
            }
            finally
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true).ConfigureAwait(false);
                }
            }
        }
    }
}