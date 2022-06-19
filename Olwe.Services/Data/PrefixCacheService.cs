using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Olwe.Data.MediatR;
using Remora.Rest.Core;

namespace Olwe.Services.Data;

[PrimaryConstructor]
public partial class PrefixCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IMediator _mediator;
    private readonly ILogger<PrefixCacheService> _logger;

    public async ValueTask<string> RetrievePrefixAsync(Snowflake? guildId, CancellationToken ct = new())
    {
        if (guildId is null) return string.Empty;
        return _memoryCache.TryGetValue($"guild:{guildId.Value.Value}:prefix", out string prefix) 
            ? prefix : 
            await GetDatabasePrefixAsync(guildId.Value, ct);
    }

    private async Task<string> GetDatabasePrefixAsync(Snowflake guildId, CancellationToken ct = new())
    {
        var guildEntity = await _mediator.Send(new GetOrCreateGuildRequest(guildId), ct);
        _memoryCache.Set($"guild:{guildId.Value}:prefix", guildEntity.Prefix);
        return guildEntity.Prefix;
    }

    public void SetPrefix(Snowflake? guildId, string prefix)
    {
        if (guildId is null) return;

        var key = $"guild:{guildId.Value.Value}:prefix";
        _memoryCache.TryGetValue(key, out string oldPrefix);
        _memoryCache.Set(key, prefix);
        _logger.LogInformation("Prefix changed from {OldPrefix} to {NewPrefix} for {Id}", oldPrefix, prefix, guildId);
    }
}