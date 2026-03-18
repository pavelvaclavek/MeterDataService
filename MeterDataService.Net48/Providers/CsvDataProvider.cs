using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MeterDataService.Net48.Logging;
using MeterDataService.Net48.Models;

namespace MeterDataService.Net48.Providers
{
    public class CsvDataProvider : IDataProvider
    {
        private readonly EventLog _eventLog;
        private readonly IAppLogger _appLogger;
        private readonly ServiceConfiguration _config;
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public string Name => "csv";

        public CsvDataProvider(EventLog eventLog, IAppLogger appLogger, ServiceConfiguration config)
        {
            _eventLog = eventLog;
            _appLogger = appLogger;
            _config = config;
        }

        public async Task<bool> ProcessAsync(MeterMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var messageTime = message.GetUtcDateTime();
                var fileName = string.Format("{0}_{1:MM_yyyy}.csv", message.Sn, messageTime);
                var filePath = Path.Combine(_config.CsvOutputPath, fileName);

                Directory.CreateDirectory(_config.CsvOutputPath);

                await _fileLock.WaitAsync(cancellationToken);
                try
                {
                    var fileExists = File.Exists(filePath);
                    using (var writer = new StreamWriter(filePath, true, Encoding.UTF8))
                    {
                        if (!fileExists)
                        {
                            await writer.WriteLineAsync("Timestamp;Sn;Id;Network;Model;Data_1_8_0;Data_1_8_1;Data_1_8_2;Data_2_8_0;RawData");
                        }

                        var parsedData = ParseMeterData(message.Result.Data);
                        var line = string.Join(";",
                            messageTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                            message.Sn,
                            message.Id,
                            message.Network,
                            message.Model,
                            GetValueOrDefault(parsedData, "1.8.0", ""),
                            GetValueOrDefault(parsedData, "1.8.1", ""),
                            GetValueOrDefault(parsedData, "1.8.2", ""),
                            GetValueOrDefault(parsedData, "2.8.0", ""),
                            EscapeCsvField(message.Result.Data)
                        );

                        await writer.WriteLineAsync(line);
                        _eventLog.WriteEntry(
                            string.Format("Data saved to CSV: {0}, SN: {1}", fileName, message.Sn),
                            EventLogEntryType.Information);
                        return true;
                    }
                }
                finally
                {
                    _fileLock.Release();
                }
            }
            catch (Exception ex)
            {
                _eventLog.WriteEntry(
                    string.Format("Error saving data to CSV for SN: {0}: {1}", message.Sn, ex),
                    EventLogEntryType.Error);
                await _appLogger.LogErrorAsync(
                    string.Format("Error saving data to CSV for SN: {0}", message.Sn),
                    ex, "Provider.csv");
                return false;
            }
        }

        private static Dictionary<string, string> ParseMeterData(string data)
        {
            var result = new Dictionary<string, string>();
            var lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"(\d+\.\d+\.\d+)\(([^)]+)\)");
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

        private static string EscapeCsvField(string field)
        {
            return string.Format("\"{0}\"", field.Replace("\"", "\"\"").Replace("\r\n", " ").Replace("\n", " "));
        }

        private static string GetValueOrDefault(Dictionary<string, string> dict, string key, string defaultValue)
        {
            string value;
            return dict.TryGetValue(key, out value) ? value : defaultValue;
        }
    }
}
