using System.Data.Entity;

namespace MeterDataService.Net48.Data
{
    public class MeterDataContext : DbContext
    {
        public MeterDataContext() : base("name=SqlServer")
        {
        }

        public DbSet<MeterReading> MeterReadings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MeterReading>()
                .ToTable("MeterReadings");

            modelBuilder.Entity<MeterReading>()
                .Property(e => e.Data_1_8_0).HasPrecision(18, 4);
            modelBuilder.Entity<MeterReading>()
                .Property(e => e.Data_1_8_1).HasPrecision(18, 4);
            modelBuilder.Entity<MeterReading>()
                .Property(e => e.Data_1_8_2).HasPrecision(18, 4);
            modelBuilder.Entity<MeterReading>()
                .Property(e => e.Data_2_8_0).HasPrecision(18, 4);
        }
    }
}
