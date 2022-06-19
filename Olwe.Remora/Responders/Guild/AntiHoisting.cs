using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;

namespace Olwe.Remora.Responders.Guild;

[PrimaryConstructor]
public partial class AntiHoisting : IResponder<IGuildMemberUpdate>
{
    private readonly IDiscordRestGuildAPI _guildApi;

    public async Task<Result> RespondAsync(IGuildMemberUpdate gatewayEvent, CancellationToken ct = new())
    {
        // TODO config
        var name = gatewayEvent.Nickname.Value ?? gatewayEvent.User.Username;
        
        if (name is "💩" || (name.Length >= 3 && name[0] is >= 'A' and <= 'Z'or >= 'a'and <= 'z' or >= '0' and <= '9'))
            return Result.FromSuccess();
        
        var result = await _guildApi.ModifyGuildMemberAsync(gatewayEvent.GuildID, gatewayEvent.User.ID, "💩", ct: ct);
        return result.IsSuccess ? Result.FromSuccess() : Result.FromError(result.Error);
    }
}