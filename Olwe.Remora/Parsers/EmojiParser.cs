using System.Text.RegularExpressions;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Olwe.Remora.Parsers;

[PrimaryConstructor]
public partial class EmojiParser : AbstractTypeParser<IPartialEmoji>
{
    private readonly Regex _emojiRegex = new(@"<(?<ANIMATED>a)?:(?<NAME>[a-zA-Z0-9_]+):(?<ID>[0-9]+)>", RegexOptions.Compiled);
    
    private readonly ITypeParser<IEmoji> _emojiParser;

    public override async ValueTask<Result<IPartialEmoji>> TryParseAsync(string token, CancellationToken ct = default)
    {
        var baseMatch = await _emojiParser.TryParseAsync(token, ct);
        
        if (baseMatch.IsSuccess)
            return Result<IPartialEmoji>.FromSuccess(baseMatch.Entity);

        var match = _emojiRegex.Match(token);
        
        if (!match.Success)
            return Result<IPartialEmoji>.FromError(new ParsingError<IPartialEmoji>("Unable to parse emoji."));
        
        if (!Snowflake.TryParse(match.Groups["ID"].Value, out var snowflake))
            return Result<IPartialEmoji>.FromError(new ParsingError<IPartialEmoji>("Unable to parse emoji."));
        
        var isAnimated = match.Groups["ANIMATED"].Success;
        var emojiName = match.Groups["NAME"].Value;
        
        return Result<IPartialEmoji>.FromSuccess(new PartialEmoji(snowflake.Value, emojiName, IsAnimated: isAnimated));
    }
}