using MeterDataService.Data;
using Microsoft.EntityFrameworkCore;

namespace MeterDataService.Logging;

/// <summary>
/// Implementace aplikaèního loggeru do SQLite databáze.
/// </summary>
public class SqliteAppLogger : IAppLogger
{
    private readonly IDbContextFactory<SqliteMeterDataContext> _contextFactory;
    private readonly ILogger<SqliteAppLogger> _logger;

    public SqliteAppLogger(
        IDbContextFactory<SqliteMeterDataContext> contextFactory,
        ILogger<SqliteAppLogger> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public Task LogInformationAsync(string message, string? type = null, string? ip = null)
        => WriteLogAsync("Information", message, null, type, ip);

    public Task LogWarningAsync(string message, string? type = null, string? ip = null)
        => WriteLogAsync("Warning", message, null, type, ip);

    public Task LogErrorAsync(string message, Exception? exception = null, string? type = null, string? ip = null)
    {
        var fullMessage = exception != null
            ? $"{message} | Exception: {exception.GetType().Name}: {exception.Message}"
            : message;

        return WriteLogAsync("Error", fullMessage, exception, type, ip);
    }

    private async Task WriteLogAsync(string severity, string message, Exception? exception, string? type, string? ip)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var logEntry = new AppLogEntry
            {
                Message = message,
                LogDate = DateTime.UtcNow,
                IP = ip,
                Severity = severity,
                Type = type
            };

            context.Logs.Add(logEntry);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Fallback - logujeme do standardního loggeru, aby se zabránilo nekoneèné smyèce
            _logger.LogError(ex, "Failed to write log to SQLite: {Message}", message);
        }
    }
}
