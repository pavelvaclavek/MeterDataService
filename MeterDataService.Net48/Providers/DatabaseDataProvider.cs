using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MeterDataService.Net48.Data;
using MeterDataService.Net48.Logging;
using MeterDataService.Net48.Models;

namespace MeterDataService.Net48.Providers
{
    public class DatabaseDataProvider : IDataProvider
    {
        private static readonly Regex MeterDataRegex = new Regex(@"(\d+\.\d+\.\d+)\(([^)]+)\)", RegexOptions.Compiled);

        private readonly EventLog _eventLog;
        private readonly IAppLogger _appLogger;

        public string Name => "database";

        public DatabaseDataProvider(EventLog eventLog, IAppLogger appLogger)
        {
            _eventLog = eventLog;
            _appLogger = appLogger;
        }

        public async Task<bool> ProcessAsync(MeterMessage message, CancellationToken cancellationToken)
        {
            try
            {
                using (var context = new MeterDataContext())
                {
                    var messageTime = message.GetUtcDateTime();
                    var parsedData = ParseMeterData(message.Result.Data);

                    var reading = new MeterReading
                    {
                        SerialNumber = message.Sn,
                        Timestamp = messageTime,
                        MessageId = message.Id,
                        Network = message.Network,
                        Model = message.Model,
                        System = message.System,
                        Data_1_8_0 = ParseDecimal(GetValueOrDefault(parsedData, "1.8.0")),
                        Data_1_8_1 = ParseDecimal(GetValueOrDefault(parsedData, "1.8.1")),
                        Data_1_8_2 = ParseDecimal(GetValueOrDefault(parsedData, "1.8.2")),
                        Data_2_8_0 = ParseDecimal(GetValueOrDefault(parsedData, "2.8.0")),
                        RawData = message.Result.Data,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.MeterReadings.Add(reading);
                    await context.SaveChangesAsync(cancellationToken);

                    _eventLog.WriteEntry(
                        string.Format("Data saved to database: SN={0}, Timestamp={1}, Id={2}", message.Sn, messageTime, reading.Id),
                        EventLogEntryType.Information);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _eventLog.WriteEntry(
                    string.Format("Error saving data to database for SN: {0}: {1}", message.Sn, ex),
                    EventLogEntryType.Error);
                await _appLogger.LogErrorAsync(
                    string.Format("Error saving data to database for SN: {0}", message.Sn),
                    ex, "Provider.database");
                return false;
            }
        }

        private static Dictionary<string, string> ParseMeterData(string data)
        {
            var result = new Dictionary<string, string>();
            var lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var match = MeterDataRegex.Match(line);
                if (match.Success)
                {
                    var key = match.Groups[1].Value;
                    var value = match.Groups[2].Value
                        .Replace("*kWh", "")
                        .Replace("#kWh", "")
                        .Trim();
                    result[key] = value;
                }
            }

            return result;
        }

        private static decimal? ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            decimal result;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
                ? result
                : (decimal?)null;
        }

        private static string GetValueOrDefault(Dictionary<string, string> dict, string key)
        {
            string value;
            return dict.TryGetValue(key, out value) ? value : null;
        }
    }
}
