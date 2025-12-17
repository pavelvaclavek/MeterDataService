using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MeterDataService.Data;

public class MeterDataContextFactory : IDesignTimeDbContextFactory<MeterDataContext>
{
    public MeterDataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MeterDataContext>();

        var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

        IConfigurationSection section =  config.GetSection("ServiceConfiguration");

        //var connectionString = section.GetValue<string>("ConnectionString");
        //optionsBuilder.UseSqlServer(connectionString);

        // VP: Connection string can be retrieved from configuration if needed
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=MeterData;Trusted_Connection=True;TrustServerCertificate=True;");

        return new MeterDataContext(optionsBuilder.Options);
    }
}