using Microsoft.EntityFrameworkCore;

namespace Olwe.Data;

public class OlweContext : DbContext
{
    public OlweContext(DbContextOptions<OlweContext> options) 
        : base(options) { }

    public OlweContext() { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OlweContext).Assembly);
    }
}