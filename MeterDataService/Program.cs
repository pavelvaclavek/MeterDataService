using MeterDataService.Data;
using MeterDataService.Models;
using MeterDataService.Providers;
using MeterDataService.Workers;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configuration
        builder.Services.Configure<ServiceConfiguration>(
            builder.Configuration.GetSection("ServiceConfiguration"));

        // SQL Server DbContext (pro DatabaseDataProvider)
        builder.Services.AddDbContextFactory<MeterDataContext>(options =>
        {
            var config = builder.Configuration.GetSection("ServiceConfiguration").Get<ServiceConfiguration>();
            options.UseSqlServer(config?.Database.ConnectionString ?? "");
        });

        // SQLite DbContext (pro SqliteDataProvider)
        // SQLite je souborová databáze - data se ukládají do souboru MeterData.db
        builder.Services.AddDbContextFactory<SqliteMeterDataContext>(options =>
        {
            var config = builder.Configuration.GetSection("ServiceConfiguration").Get<ServiceConfiguration>();
            var dbPath = config?.Sqlite.DatabasePath ?? "MeterData.db";
            
            // Connection string pro SQLite - "Data Source=cesta_k_souboru"
            options.UseSqlite($"Data Source={dbPath}");
        });

        // Register data providers
        builder.Services.AddSingleton<IDataProvider, CsvDataProvider>();
        builder.Services.AddSingleton<IDataProvider, EmailDataProvider>();
        // Register database providera (SQL Server)
        builder.Services.AddSingleton<IDataProvider, DatabaseDataProvider>();
        // Register SQLite providera
        builder.Services.AddSingleton<IDataProvider, SqliteDataProvider>();

        // Register provider manager
        builder.Services.AddSingleton<IDataProviderManager, DataProviderManager>();

        // Register background worker
        builder.Services.AddHostedService<TcpListenerWorker>();

        // Configure Windows Service
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "MeterDataService";
        });

        var host = builder.Build();

        // Automatická migrace pøi startu - SQL Server
        using (var scope = host.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MeterDataContext>();
            context.Database.MigrateAsync().ConfigureAwait(false);
        }

        // Automatická migrace pøi startu - SQLite
        // EnsureCreated() vytvoøí databázi a tabulky, pokud neexistují
        using (var scope = host.Services.CreateScope())
        {
            var sqliteContext = scope.ServiceProvider.GetRequiredService<SqliteMeterDataContext>();
            sqliteContext.Database.EnsureCreated();
        }

        host.Run();
    }
}