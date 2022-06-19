using System.Text.RegularExpressions;
using Olwe.Services.Data;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;

namespace Olwe.Remora;

public class PrefixMatcher : ICommandPrefixMatcher
{
    private static readonly Regex MentionRegex = new(@"^(<@!?(?<ID>\d+)> ?)", RegexOptions.Compiled);
    
    private readonly ICommandContext _context;
    private readonly IDiscordRestUserAPI _userApi;
    private readonly PrefixCacheService _prefixCacheService;

    public PrefixMatcher
    (
        ICommandContext context,
        IDiscordRestUserAPI userApi,
        PrefixCacheService prefixCacheService
    )
    {
        _context = context;
        _userApi = userApi;
        _prefixCacheService = prefixCacheService;
    }
    
    public async ValueTask<Result<(bool Matches, int ContentStartIndex)>> MatchesPrefixAsync(string content, CancellationToken ct = new())
    {
        if (string.IsNullOrEmpty(content))
            return Result<(bool Matches, int ContentStartIndex)>.FromSuccess((false, 0));
        
        if (!_context.GuildID.IsDefined(out var guildId))
            return Result<(bool Matches, int ContentStartIndex)>.FromSuccess((true, 0));

        var prefix = await _prefixCacheService.RetrievePrefixAsync(guildId, ct);
        
        if (content.StartsWith(prefix))
            return Result<(bool Matches, int ContentStartIndex)>.FromSuccess((true, prefix.Length));
        
        var selfResult = await _userApi.GetCurrentUserAsync(ct);
        
        if (!selfResult.IsSuccess)
            return Result<(bool Matches, int ContentStartIndex)>.FromError(selfResult.Error);
        
        var match = MentionRegex.Match(content);
        
        if (match.Success && match.Groups["ID"].Value == selfResult.Entity.ID.ToString())
            return Result<(bool Matches, int ContentStartIndex)>.FromSuccess((true, match.Length));
        
        return Result<(bool Matches, int ContentStartIndex)>.FromSuccess((false, 0));
    }
}