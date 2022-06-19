using System.ComponentModel.DataAnnotations;
using Remora.Rest.Core;

namespace Olwe.Data.Entities.Guild;

public class GuildConfigEntity
{
    [Key]
    public Snowflake GuildId { get; set; }
    
    public GuildEntity Guild { get; set; } = null!;
}