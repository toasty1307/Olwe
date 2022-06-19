using System.Drawing;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;

namespace Olwe.Remora.PostExecutionEvents;

[PrimaryConstructor]
public partial class CommandExecuted : IPostExecutionEvent
{
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestInteractionAPI _interactionApi;

    public async Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult,
        CancellationToken ct = new())
    {
        if (commandResult.IsSuccess)
            return Result.FromSuccess();

        var embed = new Embed
        {
            Title = commandResult.Error.Message,
            Colour = Color.Red
        };

        if (context is InteractionContext interactionContext)
        {
            return (Result) await _interactionApi.EditOriginalInteractionResponseAsync(interactionContext.ApplicationID,
                interactionContext.Token, embeds: new[] {embed}, ct: ct);
        }

        var messageContext = context as MessageContext;
        return (Result) await _channelApi.CreateMessageAsync(context.ChannelID, embeds: new[] {embed},
            messageReference: new MessageReference(messageContext!.MessageID, context.ChannelID), ct: ct);
    }
}