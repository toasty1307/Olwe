using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using Microsoft.EntityFrameworkCore;
using Olwe.Data;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Results;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Gateway;

namespace Olwe.Remora.Commands.General;

[Category("General")]
[PrimaryConstructor]
public partial class PingCommand : BaseCommandGroup
{
    private readonly OlweContext _db;
    private readonly ICommandContext _context;
    private readonly DiscordGatewayClient _gateway;

    private readonly IDiscordRestChannelAPI _channels;
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private InteractionContext? InteractionContext => _context as InteractionContext;
    private MessageContext? MessageContext => _context as MessageContext;
    
    [Command("ping")]
    [Description("Shows the latency of the bot")]
    [Ephemeral]
    public async Task<Result<IMessage>> Ping()
    {
        var embed = new Embed
        {
            Colour = Color.DodgerBlue,
            Fields = new EmbedField[]
            {
                new("→ Message Latency ←", $"```cs\n{"Fetching..".PadLeft(15, '⠀')}```", true),
                new("​", "​", true),
                new("→ Websocket latency ←", $"```cs\n{"Fetching..".PadLeft(15, '⠀')}```", true),

                new("→ Database Latency ←", $"```cs\n{"Fetching..".PadLeft(15, '⠀')}```", true),
                new("​", "​", true),
                new("→ Discord API Latency ←", $"```cs\n{"Fetching..".PadLeft(15, '⠀')}```", true)
            }
        };

        Result<IMessage> message;
        if (InteractionContext is not null)
        {
            message = await _interactionApi.EditOriginalInteractionResponseAsync(InteractionContext.ApplicationID,
                InteractionContext.Token, embeds: new[]{embed});
        }
        else
        {
            message = await _channels.CreateMessageAsync(_context.ChannelID, embeds: new[] {embed});
        }

        if (!message.IsSuccess)
            return message;

        var sw = Stopwatch.StartNew();
        var typing = await _channels.TriggerTypingIndicatorAsync(_context.ChannelID);
        sw.Stop();

        if (!typing.IsSuccess)
            return Result<IMessage>.FromError(typing.Error);

        var apiLat = sw.ElapsedMilliseconds.ToString("N0");

        var dbLat = await GetDbLatency();
        var messageLat = message.Entity.Timestamp -
                         (MessageContext?.Message.Timestamp.Value ?? InteractionContext?.ID.Timestamp);
        embed = embed with
        {
            Fields = new[]
            {
                (embed.Fields.Value[0] as EmbedField)! with
                {
                    Value = $"```cs\n{$"{messageLat?.TotalMilliseconds:N0} ms",10}```"
                },
                (embed.Fields.Value[1] as EmbedField)!,
                (embed.Fields.Value[2] as EmbedField)! with
                {
                    Value = $"```cs\n{$"{_gateway.Latency.TotalMilliseconds:N0} ms",10}```"
                },
                (embed.Fields.Value[3] as EmbedField)! with {Value = $"```cs\n{$"{dbLat}",7} ms```"},
                (embed.Fields.Value[4] as EmbedField)!,
                (embed.Fields.Value[5] as EmbedField)! with {Value = $"```cs\n{$"{apiLat} ms",11}```"},
            }
        };

        if (InteractionContext is not null)
        {
            return await _interactionApi.EditOriginalInteractionResponseAsync(InteractionContext.ApplicationID,
                InteractionContext.Token, embeds: new[] {embed});
        }
        return await _channels.EditMessageAsync(_context.ChannelID, message.Entity.ID, embeds: new[] {embed});
    }

    private async Task<int> GetDbLatency()
    {
        var sw = Stopwatch.StartNew();
        await _db.Database.ExecuteSqlRawAsync("SELECT first_value(\"Id\") OVER () FROM \"Guilds\"");
        sw.Stop();
        return (int) sw.ElapsedMilliseconds;
    }
}