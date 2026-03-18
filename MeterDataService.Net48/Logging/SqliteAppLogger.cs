using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MeterDataService.Net48.Logging
{
    /// <summary>
    /// Implementace aplikaèního loggeru do SQLite databáze pro .NET Framework 4.8.
    /// </summary>
    public class SqliteAppLogger : IAppLogger
    {
        private readonly EventLog _eventLog;
        private readonly string _connectionString;
        private bool _initialized;
        private readonly object _initLock = new object();

        public SqliteAppLogger(EventLog eventLog, string databasePath)
        {
            _eventLog = eventLog;
            _connectionString = string.Format("Data Source={0};Version=3;", databasePath);
        }

        public Task LogInformationAsync(string message, string type = null, string ip = null)
            => WriteLogAsync("Information", message, null, type, ip);

        public Task LogWarningAsync(string message, string type = null, string ip = null)
            => WriteLogAsync("Warning", message, null, type, ip);

        public Task LogErrorAsync(string message, Exception exception = null, string type = null, string ip = null)
        {
            var fullMessage = exception != null
                ? string.Format("{0} | Exception: {1}: {2}", message, exception.GetType().Name, exception.Message)
                : message;

            return WriteLogAsync("Error", fullMessage, exception, type, ip);
        }

        private async Task WriteLogAsync(string severity, string message, Exception exception, string type, string ip)
        {
            try
            {
                EnsureInitialized();

                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"INSERT INTO Logs (Message, LogDate, IP, AppID, Severity, Type)
                                VALUES (@Message, @LogDate, @IP, @AppID, @Severity, @Type)";

                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Message", message);
                        cmd.Parameters.AddWithValue("@LogDate", DateTime.UtcNow.ToString("o"));
                        cmd.Parameters.AddWithValue("@IP", (object)ip ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AppID", "MeterDataService.Net48");
                        cmd.Parameters.AddWithValue("@Severity", severity);
                        cmd.Parameters.AddWithValue("@Type", (object)type ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback - logujeme do EventLog, aby se zabránilo nekoneèné smyèce
                _eventLog.WriteEntry(
                    string.Format("Failed to write log to SQLite: {0}. Original message: {1}", ex.Message, message),
                    EventLogEntryType.Error);
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            lock (_initLock)
            {
                if (_initialized) return;

                var dbPath = _connectionString
                    .Replace("Data Source=", "")
                    .Replace(";Version=3;", "");

                var dbDir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDir))
                    Directory.CreateDirectory(dbDir);

                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    var sql = @"CREATE TABLE IF NOT EXISTS Logs (
                        LogID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Message TEXT NOT NULL,
                        LogDate TEXT NOT NULL,
                        IP TEXT,
                        AppID TEXT DEFAULT 'MeterDataService.Net48',
                        Severity TEXT NOT NULL,
                        Type TEXT
                    );
                    CREATE INDEX IF NOT EXISTS IX_Logs_LogDate ON Logs(LogDate);
                    CREATE INDEX IF NOT EXISTS IX_Logs_Severity ON Logs(Severity);
                    CREATE INDEX IF NOT EXISTS IX_Logs_Type ON Logs(Type);
                    CREATE INDEX IF NOT EXISTS IX_Logs_Severity_LogDate ON Logs(Severity, LogDate);";

                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                _initialized = true;
            }
        }
    }
}
