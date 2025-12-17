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

        builder.Services.AddDbContextFactory<MeterDataContext>(options =>
        {
            var config = builder.Configuration.GetSection("ServiceConfiguration").Get<ServiceConfiguration>();
            options.UseSqlServer(config?.Database.ConnectionString ?? "");
        });

        // Register data providers
        builder.Services.AddSingleton<IDataProvider, CsvDataProvider>();
        builder.Services.AddSingleton<IDataProvider, EmailDataProvider>();
        // Register database providera
        builder.Services.AddSingleton<IDataProvider, DatabaseDataProvider>();
       

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

        // Automatická migrace pøi startu
        using (var scope = host.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MeterDataContext>();
            context.Database.MigrateAsync().ConfigureAwait(false);
        }

        host.Run();
    }
}