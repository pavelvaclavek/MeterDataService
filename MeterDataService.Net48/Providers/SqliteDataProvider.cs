using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MeterDataService.Net48.Models;

namespace MeterDataService.Net48.Providers
{
    public class SqliteDataProvider : IDataProvider
    {
        private static readonly Regex MeterDataRegex = new Regex(@"(\d+\.\d+\.\d+)\(([^)]+)\)", RegexOptions.Compiled);

        private readonly EventLog _eventLog;
        private readonly ServiceConfiguration _config;
        private readonly string _connectionString;
        private bool _initialized;
        private readonly object _initLock = new object();

        public string Name => "sqlite";

        public SqliteDataProvider(EventLog eventLog, ServiceConfiguration config)
        {
            _eventLog = eventLog;
            _config = config;
            _connectionString = string.Format("Data Source={0};Version=3;", config.Sqlite.DatabasePath);
        }

        public async Task<bool> ProcessAsync(MeterMessage message, CancellationToken cancellationToken)
        {
            try
            {
                EnsureInitialized();

                var messageTime = message.GetUtcDateTime();
                var parsedData = ParseMeterData(message.Result.Data);

                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync(cancellationToken);

                    var sql = @"INSERT INTO MeterReadings
                        (SerialNumber, Timestamp, MessageId, Network, Model, System,
                         Data_1_8_0, Data_1_8_1, Data_1_8_2, Data_2_8_0, RawData, CreatedAt)
                        VALUES
                        (@SerialNumber, @Timestamp, @MessageId, @Network, @Model, @System,
                         @Data_1_8_0, @Data_1_8_1, @Data_1_8_2, @Data_2_8_0, @RawData, @CreatedAt)";

                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@SerialNumber", message.Sn);
                        cmd.Parameters.AddWithValue("@Timestamp", messageTime.ToString("o"));
                        cmd.Parameters.AddWithValue("@MessageId", (object)message.Id ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Network", (object)message.Network ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Model", (object)message.Model ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@System", (object)message.System ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Data_1_8_0", ParseDoubleOrDbNull(GetValueOrDefault(parsedData, "1.8.0")));
                        cmd.Parameters.AddWithValue("@Data_1_8_1", ParseDoubleOrDbNull(GetValueOrDefault(parsedData, "1.8.1")));
                        cmd.Parameters.AddWithValue("@Data_1_8_2", ParseDoubleOrDbNull(GetValueOrDefault(parsedData, "1.8.2")));
                        cmd.Parameters.AddWithValue("@Data_2_8_0", ParseDoubleOrDbNull(GetValueOrDefault(parsedData, "2.8.0")));
                        cmd.Parameters.AddWithValue("@RawData", (object)message.Result.Data ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));

                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }

                _eventLog.WriteEntry(
                    string.Format("Data saved to SQLite: SN={0}, Timestamp={1}, DbPath={2}", message.Sn, messageTime, _config.Sqlite.DatabasePath),
                    EventLogEntryType.Information);

                return true;
            }
            catch (Exception ex)
            {
                _eventLog.WriteEntry(
                    string.Format("Error saving data to SQLite for SN: {0}: {1}", message.Sn, ex),
                    EventLogEntryType.Error);
                return false;
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            lock (_initLock)
            {
                if (_initialized) return;

                var dbDir = Path.GetDirectoryName(_config.Sqlite.DatabasePath);
                if (!string.IsNullOrEmpty(dbDir))
                    Directory.CreateDirectory(dbDir);

                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    var sql = @"CREATE TABLE IF NOT EXISTS MeterReadings (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SerialNumber TEXT NOT NULL,
                        Timestamp TEXT NOT NULL,
                        MessageId TEXT,
                        Network TEXT,
                        Model TEXT,
                        System TEXT,
                        Data_1_8_0 REAL,
                        Data_1_8_1 REAL,
                        Data_1_8_2 REAL,
                        Data_2_8_0 REAL,
                        RawData TEXT,
                        CreatedAt TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_MeterReadings_SerialNumber ON MeterReadings(SerialNumber);
                    CREATE INDEX IF NOT EXISTS IX_MeterReadings_Timestamp ON MeterReadings(Timestamp);
                    CREATE INDEX IF NOT EXISTS IX_MeterReadings_SerialNumber_Timestamp ON MeterReadings(SerialNumber, Timestamp);";

                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                _initialized = true;
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

        private static object ParseDoubleOrDbNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DBNull.Value;

            double result;
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
                ? (object)result
                : DBNull.Value;
        }

        private static string GetValueOrDefault(Dictionary<string, string> dict, string key)
        {
            string value;
            return dict.TryGetValue(key, out value) ? value : null;
        }
    }
}
