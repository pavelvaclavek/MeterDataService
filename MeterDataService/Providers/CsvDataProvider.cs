using System.Globalization;
using System.Text;
using MeterDataService.Models;
using Microsoft.Extensions.Options;

namespace MeterDataService.Providers
{
    public class CsvDataProvider : IDataProvider
    {
        private readonly ILogger<CsvDataProvider> _logger;
        private readonly ServiceConfiguration _config;
        private readonly SemaphoreSlim _fileLock = new(1, 1);

        public string Name => "csv";

        public CsvDataProvider(ILogger<CsvDataProvider> logger, IOptions<ServiceConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task<bool> ProcessAsync(MeterMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var messageTime = message.GetUtcDateTime();
                var fileName = $"{message.Sn}_{messageTime:MM_yyyy}.csv";
                var filePath = Path.Combine(_config.CsvOutputPath, fileName);

                Directory.CreateDirectory(_config.CsvOutputPath);

                await _fileLock.WaitAsync(cancellationToken);
                try
                {
                    var fileExists = File.Exists(filePath);
                    await using var writer = new StreamWriter(filePath, append: true, Encoding.UTF8);

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
                        parsedData.GetValueOrDefault("1.8.0", ""),
                        parsedData.GetValueOrDefault("1.8.1", ""),
                        parsedData.GetValueOrDefault("1.8.2", ""),
                        parsedData.GetValueOrDefault("2.8.0", ""),
                        EscapeCsvField(message.Result.Data)
                    );

                    await writer.WriteLineAsync(line);
                    _logger.LogInformation("Data saved to CSV: {FileName}, SN: {Sn}", fileName, message.Sn);
                    return true;
                }
                finally
                {
                    _fileLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving data to CSV for SN: {Sn}", message.Sn);
                return false;
            }
        }

        private static Dictionary<string, string> ParseMeterData(string data)
        {
            var result = new Dictionary<string, string>();
            var lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+\.\d+\.\d+)\(([^)]+)\)");
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
            return $"\"{field.Replace("\"", "\"\"").Replace("\r\n", " ").Replace("\n", " ")}\"";
        }
    }
}