using System.Net;
using System.Net.Mail;
using System.Text;
using MeterDataService.Models;
using Microsoft.Extensions.Options;

namespace MeterDataService.Providers
{
    public class EmailDataProvider : IDataProvider
    {
        private readonly ILogger<EmailDataProvider> _logger;
        private readonly ServiceConfiguration _config;

        public string Name => "email";

        public EmailDataProvider(ILogger<EmailDataProvider> logger, IOptions<ServiceConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task<bool> ProcessAsync(MeterMessage message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_config.Email.SmtpServer))
            {
                _logger.LogWarning("Email provider is enabled but SMTP server is not configured");
                return false;
            }

            try
            {
                using var client = new SmtpClient(_config.Email.SmtpServer, _config.Email.SmtpPort)
                {
                    EnableSsl = _config.Email.UseSsl,
                    Credentials = new NetworkCredential(_config.Email.Username, _config.Email.Password)
                };

                var messageTime = message.GetUtcDateTime();
                var subject = $"Meter Data - SN: {message.Sn} - {messageTime:yyyy-MM-dd HH:mm}";
                var body = BuildEmailBody(message, messageTime);

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_config.Email.FromAddress),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                foreach (var to in _config.Email.ToAddresses)
                {
                    mailMessage.To.Add(to);
                }

                await client.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Email sent for SN: {Sn}", message.Sn);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email for SN: {Sn}", message.Sn);
                return false;
            }
        }

        private static string BuildEmailBody(MeterMessage message, DateTime messageTime)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Meter Data Report");
            sb.AppendLine("=================");
            sb.AppendLine();
            sb.AppendLine($"Timestamp: {messageTime:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Serial Number: {message.Sn}");
            sb.AppendLine($"Model: {message.Model}");
            sb.AppendLine($"Network: {message.Network}");
            sb.AppendLine($"ID: {message.Id}");
            sb.AppendLine();
            sb.AppendLine("Raw Data:");
            sb.AppendLine(message.Result.Data);
            return sb.ToString();
        }
    }
}
