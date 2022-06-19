using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Olwe.Data.Entities.Guild;
using Olwe.Data.MediatR;
using Remora.Rest.Core;

namespace Olwe.Services.Data;

[PrimaryConstructor]
public partial class ModConfigCacheService
{
    private readonly ILogger<ModConfigCacheService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IMediator _mediator;

    public async ValueTask<GuildModConfigEntity> GetModConfigAsync(Snowflake? guildId,
        CancellationToken cancellationToken = new())
    {
        if (guildId is null) return new GuildModConfigEntity();
        
        var key = $"guild:{guildId}:modconfig";
        var value = _cache.Get<GuildModConfigEntity>(key);
        if (value is not null) return value;
        
        var request = new GetOrCreateModConfigRequest(guildId.Value);
        var config = await _mediator.Send(request, cancellationToken);
        _cache.Set(key, config);

        return config;
    }
    
    public void SetModConfig(GuildModConfigEntity config)
    {
        var key = $"guild:{config.GuildId}:modconfig";
        _cache.Set(key, config);
    }
}