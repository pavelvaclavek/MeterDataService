using MeterDataService.Data;
using MeterDataService.Logging;
using MeterDataService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MeterDataService.Providers;

/// <summary>
/// Provider pro ukládání dat do SQLite databáze.
/// 
/// SQLite je lehká souborová databáze:
/// - Nepotøebuje server (na rozdíl od SQL Server, MySQL, PostgreSQL)
/// - Data se ukládají do jednoho souboru (napø. MeterData.db)
/// - Ideální pro menší aplikace, embedded systémy, nebo lokální úložištì
/// - Podporuje vìtšinu SQL pøíkazù
/// 
/// Omezení SQLite:
/// - Nepodporuje typ decimal (používáme double/REAL)
/// - Omezená podpora pro soubìžné zápisy
/// - Není vhodná pro vysokou zátìž (tisíce zápisù/s)
/// </summary>
public partial class SqliteDataProvider : IDataProvider
{
    private readonly ILogger<SqliteDataProvider> _logger;
    private readonly IAppLogger _appLogger;
    private readonly IDbContextFactory<SqliteMeterDataContext> _contextFactory;
    private readonly ServiceConfiguration _config;

    /// <summary>
    /// Název providera - používá se v konfiguraci EnabledProviders.
    /// </summary>
    public string Name => "sqlite";

    public SqliteDataProvider(
        ILogger<SqliteDataProvider> logger,
        IAppLogger appLogger,
        IDbContextFactory<SqliteMeterDataContext> contextFactory,
        IOptions<ServiceConfiguration> config)
    {
        _logger = logger;
        _appLogger = appLogger;
        _contextFactory = contextFactory;
        _config = config.Value;
    }

    /// <summary>
    /// Zpracuje zprávu z elektromìru a uloží ji do SQLite databáze.
    /// </summary>
    public async Task<bool> ProcessAsync(MeterMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // Vytvoøení nového DbContextu pro tuto operaci
            // IDbContextFactory je thread-safe a vytváøí nový context pro každé volání
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // Parsování èasové znaèky z UTC timestamp
            var messageTime = message.GetUtcDateTime();
            
            // Parsování namìøených hodnot z textových dat
            var parsedData = ParseMeterData(message.Result.Data);

            // Vytvoøení nového záznamu
            var reading = new SqliteMeterReading
            {
                SerialNumber = message.Sn,
                Timestamp = messageTime,
                MessageId = message.Id,
                Network = message.Network,
                Model = message.Model,
                System = message.System,
                // Parsování hodnot z registrù elektromìru
                Data_1_8_0 = ParseDouble(parsedData.GetValueOrDefault("1.8.0")),
                Data_1_8_1 = ParseDouble(parsedData.GetValueOrDefault("1.8.1")),
                Data_1_8_2 = ParseDouble(parsedData.GetValueOrDefault("1.8.2")),
                Data_2_8_0 = ParseDouble(parsedData.GetValueOrDefault("2.8.0")),
                RawData = message.Result.Data,
                CreatedAt = DateTime.UtcNow
            };

            // Pøidání záznamu do kontextu
            context.MeterReadings.Add(reading);
            
            // Uložení do databáze
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Data saved to SQLite: SN={Sn}, Timestamp={Timestamp}, Id={ReadingId}, DbPath={DbPath}",
                message.Sn, messageTime, reading.Id, _config.Sqlite.DatabasePath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving data to SQLite for SN: {Sn}", message.Sn);
            await _appLogger.LogErrorAsync(
                $"Error saving data to SQLite for SN: {message.Sn}", ex, "Provider.sqlite");
            return false;
        }
    }

    /// <summary>
    /// Parsuje textová data z elektromìru do slovníku.
    /// Formát dat: "1.8.0(0000123.4*kWh)"
    /// </summary>
    private static Dictionary<string, string> ParseMeterData(string data)
    {
        var result = new Dictionary<string, string>();
        var lines = data.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Regex: èíslo registru (1.8.0) a hodnota v závorce
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

    /// <summary>
    /// Parsuje textovou hodnotu na double (SQLite nepodporuje decimal).
    /// </summary>
    private static double? ParseDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    /// <summary>
    /// Regex pro parsování dat z elektromìru.
    /// Pøíklad: "1.8.0(0000123.4*kWh)" -> groups: ["1.8.0", "0000123.4*kWh"]
    /// </summary>
    [GeneratedRegex(@"(\d+\.\d+\.\d+)\(([^)]+)\)")]
    private static partial Regex MeterDataRegex();
}
