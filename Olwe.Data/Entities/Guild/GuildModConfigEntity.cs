using System.ComponentModel.DataAnnotations;
using Remora.Rest.Core;

namespace Olwe.Data.Entities.Guild;

public class GuildModConfigEntity
{
    [Key]
    public Snowflake GuildId { get; set; }

    public GuildEntity Guild { get; set; } = null!;
    
    public bool AntiPhishingEnabled { get; set; }
    
    public InfractionType PhishingInfractionType { get; set; }
}