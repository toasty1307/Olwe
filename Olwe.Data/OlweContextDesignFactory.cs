using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Olwe.Data;

public class OlweContextDesignFactory : IDesignTimeDbContextFactory<OlweContext>
{
    public OlweContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", true)
            .AddJsonFile("appsettings.Production.json", true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<OlweContext>();
        optionsBuilder.UseNpgsql(config["Olwe:Data:ConnectionString"] ??
                                 throw new InvalidOperationException("ConnectionString not found"));

        return new OlweContext(optionsBuilder.Options);
    }
}