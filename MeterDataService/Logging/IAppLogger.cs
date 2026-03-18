namespace MeterDataService.Logging;

/// <summary>
/// Rozhraní pro aplikaèní logger do databáze.
/// </summary>
public interface IAppLogger
{
    Task LogInformationAsync(string message, string? type = null, string? ip = null);
    Task LogWarningAsync(string message, string? type = null, string? ip = null);
    Task LogErrorAsync(string message, Exception? exception = null, string? type = null, string? ip = null);
}
