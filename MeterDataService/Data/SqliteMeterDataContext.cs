using Microsoft.EntityFrameworkCore;

namespace MeterDataService.Data;

/// <summary>
/// DbContext pro SQLite databázi.
/// SQLite je lehká souborová databáze - nepotřebujete žádný server.
/// Data se ukládají do jednoho souboru (např. MeterData.db).
/// </summary>
public class SqliteMeterDataContext : DbContext
{
    public SqliteMeterDataContext(DbContextOptions<SqliteMeterDataContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Tabulka pro ukládání naměřených hodnot z elektroměrů.
    /// </summary>
    public DbSet<SqliteMeterReading> MeterReadings => Set<SqliteMeterReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SqliteMeterReading>(entity =>
        {
            // Název tabulky v databázi
            entity.ToTable("MeterReadings");

            // Indexy pro rychlejší vyhledávání
            entity.HasIndex(e => e.SerialNumber);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.SerialNumber, e.Timestamp });
        });
    }
}
