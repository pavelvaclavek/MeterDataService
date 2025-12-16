using MeterDataService.Models;
using MeterDataService.Providers;
using MeterDataService.Workers;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configuration
        builder.Services.Configure<ServiceConfiguration>(
            builder.Configuration.GetSection("ServiceConfiguration"));

        // Register data providers
        builder.Services.AddSingleton<IDataProvider, CsvDataProvider>();
        builder.Services.AddSingleton<IDataProvider, EmailDataProvider>();
        // Add more providers here: builder.Services.AddSingleton<IDataProvider, DatabaseDataProvider>();

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
        host.Run();
    }
}