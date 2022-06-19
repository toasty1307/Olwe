using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Olwe.Data.Entities.Guild;

namespace Olwe.Data.EntityConfigurations;

public class GuildConfiguration : IEntityTypeConfiguration<GuildEntity>
{
    public void Configure(EntityTypeBuilder<GuildEntity> builder)
    {
        builder
            .HasMany(g => g.Infractions)
            .WithOne(i => i.Guild)
            .HasForeignKey(i => i.GuildId);

        builder
            .HasOne(g => g.Config)
            .WithOne(c => c.Guild)
            .HasForeignKey<GuildConfigEntity>(c => c.GuildId);

        builder
            .HasOne(g => g.ModConfig)
            .WithOne(m => m.Guild)
            .HasForeignKey<GuildModConfigEntity>(m => m.GuildId);
    }
}