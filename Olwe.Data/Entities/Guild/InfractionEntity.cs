using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Remora.Rest.Core;

namespace Olwe.Data.Entities.Guild;

public class InfractionEntity
{
    [Key] public int Id { get; set; }

    public int Case { get; set; }

    public Snowflake GuildId { get; set; }

    public GuildEntity Guild { get; set; } = null!;
    
    public Snowflake UserId { get; set; }
    
    public Snowflake EnforcerId { get; set; }

    public string Reason { get; set; } = "Not specified.";
    
    public InfractionType Type { get; set; }

    public bool UserNotified { get; set; } = false;
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset? ExpiresAt { get; set; }
    
    [NotMapped]
    public TimeSpan? Duration => !ExpiresAt.HasValue ? null : ExpiresAt.Value - CreatedAt;

}
    