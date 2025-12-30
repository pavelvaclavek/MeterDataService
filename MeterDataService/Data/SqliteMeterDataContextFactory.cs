using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MeterDataService.Data;

/// <summary>
/// Factory pro vytvoøení DbContextu pøi návrhu (design-time).
/// Potøebné pro pøíkazy: dotnet ef migrations add, dotnet ef database update
/// </summary>
public class SqliteMeterDataContextFactory : IDesignTimeDbContextFactory<SqliteMeterDataContext>
{
    public SqliteMeterDataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteMeterDataContext>();
        
        // Cesta k SQLite databázi - použijte stejnou jako v appsettings.json
        optionsBuilder.UseSqlite("Data Source=MeterData.db");

        return new SqliteMeterDataContext(optionsBuilder.Options);
    }
}
