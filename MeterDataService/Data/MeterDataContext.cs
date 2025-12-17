using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace MeterDataService.Data;

public class MeterDataContext : DbContext
{
    public MeterDataContext(DbContextOptions<MeterDataContext> options)
        : base(options)
    {
    }

    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MeterReading>(entity =>
        {
            entity.HasIndex(e => e.SerialNumber);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.SerialNumber, e.Timestamp });
        });
    }
}