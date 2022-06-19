using Microsoft.EntityFrameworkCore;
using Olwe.Data.Entities.Guild;
using Remora.Rest.Core;

namespace Olwe.Data;

public class OlweContext : DbContext
{
    public DbSet<GuildEntity> Guilds { get; set; } = null!;
    public DbSet<InfractionEntity> Infractions { get; set; } = null!;
    public DbSet<GuildConfigEntity> Configs { get; set; } = null!;
    public DbSet<GuildModConfigEntity> ModConfigs { get; set; } = null!;

    public OlweContext(DbContextOptions<OlweContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OlweContext).Assembly);
    }
   
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Snowflake>().HaveConversion<SnowflakeConverter>();
        configurationBuilder.Properties<Snowflake?>().HaveConversion<NullableSnowflakeConverter>();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql();
    }
}