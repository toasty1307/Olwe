using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;

namespace Olwe.Remora.Responders.Ready;

[PrimaryConstructor]
public partial class SlashCommandRegisterer : IResponder<IReady>
{
    private readonly IConfiguration _config;
    private readonly SlashService _slash;
    private readonly IHostApplicationLifetime _lifetime;

    public Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = default)
    {
        var slashResult = _slash.SupportsSlashCommands("olwe_slash_tree");

        if (!slashResult.IsSuccess)
        {
            _lifetime.StopApplication();
        }

        var debugGuild = _config["Olwe:Discord:DebugGuild"];
        if (debugGuild is not null && ulong.TryParse(debugGuild, out var debugGuildId))
        {
            return _slash.UpdateSlashCommandsAsync(DiscordSnowflake.New(debugGuildId), "olwe_slash_tree", ct);
        }

        return _slash.UpdateSlashCommandsAsync(null, "olwe_slash_tree", ct);
    }
}