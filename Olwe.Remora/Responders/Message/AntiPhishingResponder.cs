using Microsoft.Extensions.DependencyInjection;
using Olwe.Data.Entities.Guild;
using Olwe.Services.Data;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using VTP.AntiPhishingGateway;

namespace Olwe.Remora.Responders.Message;

[PrimaryConstructor]
public partial class AntiPhishingResponder : IResponder<IMessageCreate>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ModConfigCacheService _modConfigCache;
    private readonly IDiscordRestChannelAPI _channelApi;
    
    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new())
    {
        if (!gatewayEvent.GuildID.IsDefined(out var guildId))
            return Result.FromSuccess();

        return Result.FromSuccess();

        using var scope = _scopeFactory.CreateScope();
        var phishingDetectionService = scope.ServiceProvider.GetRequiredService<PhishingDetectionService>();
        var result = await phishingDetectionService.TryDetectPhishingAsync(gatewayEvent.Content);

        if (!result.IsSuccess)
            return Result.FromError(result);
        
        if (result.Entity.IsPhishing)
        {
            var modConfig = await _modConfigCache.GetModConfigAsync(guildId, ct);
            if (!modConfig.AntiPhishingEnabled)
                return Result.FromSuccess();

            if (modConfig.PhishingInfractionType is not (InfractionType.Ban or InfractionType.SoftBan))
                await _channelApi.DeleteMessageAsync(gatewayEvent.ChannelID, gatewayEvent.ID, ct: ct);

            switch (modConfig.PhishingInfractionType)
            {
                case InfractionType.None:
                case InfractionType.Pardon:
                case InfractionType.Unban:
                case InfractionType.Unmute:
                    return Result.FromSuccess();
                case InfractionType.Strike:
                    break;
                case InfractionType.Kick:
                    break;
                case InfractionType.Mute:
                    break;
                case InfractionType.SoftBan:
                    break;
                case InfractionType.Ban:
                    break;
                case InfractionType.Note:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        return Result.FromSuccess();
    }
}