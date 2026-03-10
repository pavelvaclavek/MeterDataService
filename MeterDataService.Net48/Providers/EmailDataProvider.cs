using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MeterDataService.Net48.Models;

namespace MeterDataService.Net48.Providers
{
    public class EmailDataProvider : IDataProvider
    {
        private readonly EventLog _eventLog;
        private readonly ServiceConfiguration _config;

        public string Name => "email";

        public EmailDataProvider(EventLog eventLog, ServiceConfiguration config)
        {
            _eventLog = eventLog;
            _config = config;
        }

        public async Task<bool> ProcessAsync(MeterMessage message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_config.Email.SmtpServer))
            {
                _eventLog.WriteEntry("Email provider is enabled but SMTP server is not configured", EventLogEntryType.Warning);
                return false;
            }

            try
            {
                using (var client = new SmtpClient(_config.Email.SmtpServer, _config.Email.SmtpPort))
                {
                    client.EnableSsl = _config.Email.UseSsl;
                    client.Credentials = new NetworkCredential(_config.Email.Username, _config.Email.Password);

                    var messageTime = message.GetUtcDateTime();
                    var subject = string.Format("Meter Data - SN: {0} - {1:yyyy-MM-dd HH:mm}", message.Sn, messageTime);
                    var body = BuildEmailBody(message, messageTime);

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(_config.Email.FromAddress);
                        mailMessage.Subject = subject;
                        mailMessage.Body = body;
                        mailMessage.IsBodyHtml = false;

                        foreach (var to in _config.Email.ToAddresses)
                        {
                            mailMessage.To.Add(to);
                        }

                        // .NET 4.8 SendMailAsync nema overload s CancellationToken
                        await client.SendMailAsync(mailMessage);
                        _eventLog.WriteEntry(
                            string.Format("Email sent for SN: {0}", message.Sn),
                            EventLogEntryType.Information);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _eventLog.WriteEntry(
                    string.Format("Error sending email for SN: {0}: {1}", message.Sn, ex),
                    EventLogEntryType.Error);
                return false;
            }
        }

        private static string BuildEmailBody(MeterMessage message, DateTime messageTime)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Meter Data Report");
            sb.AppendLine("=================");
            sb.AppendLine();
            sb.AppendFormat("Timestamp: {0:yyyy-MM-dd HH:mm:ss} UTC", messageTime).AppendLine();
            sb.AppendFormat("Serial Number: {0}", message.Sn).AppendLine();
            sb.AppendFormat("Model: {0}", message.Model).AppendLine();
            sb.AppendFormat("Network: {0}", message.Network).AppendLine();
            sb.AppendFormat("ID: {0}", message.Id).AppendLine();
            sb.AppendLine();
            sb.AppendLine("Raw Data:");
            sb.AppendLine(message.Result.Data);
            return sb.ToString();
        }
    }
}
