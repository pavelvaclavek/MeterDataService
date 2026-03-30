using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using MeterDataService.Web.Models;

namespace MeterDataService.Web.Data
{
    /// <summary>
    /// Provider pro čtení dat z SQLite databáze.
    /// Poskytuje stránkování a filtrování nad tabulkou MeterReadings.
    /// </summary>
    public class SqliteDataProvider
    {
        private readonly string _connectionString;

        public SqliteDataProvider(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("Cesta k SQLite databázi nesmí být prázdná.", nameof(databasePath));

            if (!File.Exists(databasePath))
                throw new FileNotFoundException(
                    string.Format("SQLite databáze nebyla nalezena: {0}", databasePath), databasePath);

            _connectionString = string.Format("Data Source={0};Version=3;Read Only=True;", databasePath);
        }

        /// <summary>
        /// Načte stránkovaná data z tabulky MeterReadings s volitelným filtrováním.
        /// </summary>
        /// <param name="offset">Počet záznamů k přeskočení (pro stránkování).</param>
        /// <param name="limit">Maximální počet vrácených záznamů.</param>
        /// <param name="serialNumber">Filtr podle sériového čísla (volitelný, částečná shoda).</param>
        /// <param name="createdAtFrom">Filtr podle data vytvoření - od (volitelný).</param>
        /// <param name="createdAtTo">Filtr podle data vytvoření - do (volitelný).</param>
        /// <returns>Stránkovaný výsledek s daty a celkovým počtem.</returns>
        public PagedResult<MeterReadingDto> GetReadings(
            int offset,
            int limit,
            string serialNumber = null,
            DateTime? createdAtFrom = null,
            DateTime? createdAtTo = null)
        {
            // Validace vstupních parametrů
            if (offset < 0) offset = 0;
            if (limit <= 0) limit = 20;
            if (limit > 500) limit = 500; // Ochrana proti příliš velkým dotazům

            var readings = new List<MeterReadingDto>();
            int totalCount = 0;

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // Sestavení WHERE klauzule s parametrizovanými dotazy (prevence SQL injection)
                var whereClauses = new List<string>();
                var parameters = new List<SQLiteParameter>();

                if (!string.IsNullOrWhiteSpace(serialNumber))
                {
                    whereClauses.Add("SerialNumber LIKE @SerialNumber");
                    parameters.Add(new SQLiteParameter("@SerialNumber",
                        string.Format("%{0}%", serialNumber)));
                }

                if (createdAtFrom.HasValue)
                {
                    whereClauses.Add("CreatedAt >= @CreatedAtFrom");
                    parameters.Add(new SQLiteParameter("@CreatedAtFrom",
                        createdAtFrom.Value.ToString("o")));
                }

                if (createdAtTo.HasValue)
                {
                    // Posuneme konec dne na 23:59:59, aby byl zahrnut celý den
                    var endOfDay = createdAtTo.Value.Date.AddDays(1).AddSeconds(-1);
                    whereClauses.Add("CreatedAt <= @CreatedAtTo");
                    parameters.Add(new SQLiteParameter("@CreatedAtTo",
                        endOfDay.ToString("o")));
                }

                var whereClause = whereClauses.Count > 0
                    ? "WHERE " + string.Join(" AND ", whereClauses)
                    : "";

                // Nejdříve zjistíme celkový počet záznamů (pro stránkování)
                var countSql = string.Format("SELECT COUNT(*) FROM MeterReadings {0}", whereClause);

                using (var cmd = new SQLiteCommand(countSql, connection))
                {
                    foreach (var param in parameters)
                    {
                        // Parametry je nutné přidat jako nové instance (SQLite neumožňuje sdílení)
                        cmd.Parameters.AddWithValue(param.ParameterName, param.Value);
                    }
                    totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Dotaz na samotná data se stránkováním
                var dataSql = string.Format(
                    @"SELECT Id, SerialNumber, Timestamp, MessageId, Network, Model, System,
                             Data_1_8_0, Data_1_8_1, Data_1_8_2, Data_2_8_0, CreatedAt
                      FROM MeterReadings {0}
                      ORDER BY CreatedAt DESC
                      LIMIT @Limit OFFSET @Offset",
                    whereClause);

                using (var cmd = new SQLiteCommand(dataSql, connection))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.ParameterName, param.Value);
                    }
                    cmd.Parameters.AddWithValue("@Limit", limit);
                    cmd.Parameters.AddWithValue("@Offset", offset);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            readings.Add(MapReaderToDto(reader));
                        }
                    }
                }
            }

            return new PagedResult<MeterReadingDto>
            {
                Data = readings.ToArray(),
                TotalCount = totalCount,
                Offset = offset,
                Limit = limit
            };
        }

        /// <summary>
        /// Mapuje řádek z databáze na DTO objekt.
        /// </summary>
        private static MeterReadingDto MapReaderToDto(SQLiteDataReader reader)
        {
            return new MeterReadingDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                SerialNumber = GetStringOrNull(reader, "SerialNumber"),
                Timestamp = GetStringOrNull(reader, "Timestamp"),
                MessageId = GetStringOrNull(reader, "MessageId"),
                Network = GetStringOrNull(reader, "Network"),
                Model = GetStringOrNull(reader, "Model"),
                System = GetStringOrNull(reader, "System"),
                Data_1_8_0 = GetDoubleOrNull(reader, "Data_1_8_0"),
                Data_1_8_1 = GetDoubleOrNull(reader, "Data_1_8_1"),
                Data_1_8_2 = GetDoubleOrNull(reader, "Data_1_8_2"),
                Data_2_8_0 = GetDoubleOrNull(reader, "Data_2_8_0"),
                CreatedAt = GetStringOrNull(reader, "CreatedAt")
            };
        }

        /// <summary>
        /// Bezpečně načte textovou hodnotu z readeru (ošetření NULL).
        /// </summary>
        private static string GetStringOrNull(SQLiteDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        /// <summary>
        /// Bezpečně načte číselnou hodnotu z readeru (ošetření NULL).
        /// </summary>
        private static double? GetDoubleOrNull(SQLiteDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (double?)null : reader.GetDouble(ordinal);
        }
    }
}
