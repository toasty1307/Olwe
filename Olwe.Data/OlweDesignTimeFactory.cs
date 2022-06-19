using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Olwe.Data;

public class OlweDesignTimeFactory : IDesignTimeDbContextFactory<OlweContext>
{
    public OlweContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"{Directory.GetCurrentDirectory()}/../Olwe/appsettings.json")
            .Build();
        var builder = new DbContextOptionsBuilder<OlweContext>();
        var connectionString = configuration["Olwe:Data:ConnectionString"] ?? throw new InvalidOperationException("Connection string not found");
        builder.UseNpgsql(connectionString);
        return new OlweContext(builder.Options);
    }
}