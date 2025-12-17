using MeterDataService.Data;
using MeterDataService.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MeterDataService.Providers;

public partial class DatabaseDataProvider : IDataProvider
{
    private readonly ILogger<DatabaseDataProvider> _logger;
    private readonly IDbContextFactory<MeterDataContext> _contextFactory;

    public string Name => "database";

    public DatabaseDataProvider(
        ILogger<DatabaseDataProvider> logger,
        IDbContextFactory<MeterDataContext> contextFactory)
    {
        _logger = logger;
        _contextFactory = contextFactory;
    }

    public async Task<bool> ProcessAsync(MeterMessage message, CancellationToken cancellationToken)
    {
        try
        {            
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

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
                Data_1_8_0 = ParseDecimal(parsedData.GetValueOrDefault("1.8.0")),
                Data_1_8_1 = ParseDecimal(parsedData.GetValueOrDefault("1.8.1")),
                Data_1_8_2 = ParseDecimal(parsedData.GetValueOrDefault("1.8.2")),
                Data_2_8_0 = ParseDecimal(parsedData.GetValueOrDefault("2.8.0")),
                RawData = message.Result.Data,
                CreatedAt = DateTime.UtcNow
            };

            context.MeterReadings.Add(reading);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Data saved to database: SN={Sn}, Timestamp={Timestamp}, Id={ReadingId}",
                message.Sn, messageTime, reading.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving data to database for SN: {Sn}", message.Sn);
            return false;
        }
    }

    private static Dictionary<string, string> ParseMeterData(string data)
    {
        var result = new Dictionary<string, string>();
        var lines = data.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var match = MeterDataRegex().Match(line);
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

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    [GeneratedRegex(@"(\d+\.\d+\.\d+)\(([^)]+)\)")]
    private static partial Regex MeterDataRegex();
}