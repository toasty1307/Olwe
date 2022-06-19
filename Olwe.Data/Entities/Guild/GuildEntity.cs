using System.ComponentModel.DataAnnotations;
using Remora.Rest.Core;

namespace Olwe.Data.Entities.Guild;

public class GuildEntity
{
    [Key]
    public Snowflake Id { get; set; }

    public string Prefix { get; set; } = ";";

    public GuildConfigEntity Config { get; set; } = new();

    public GuildModConfigEntity ModConfig { get; set; } = new();

    public List<InfractionEntity> Infractions { get; set; } = new();
}