using System.ComponentModel;
using System.Text;
using Humanizer;
using Olwe.Remora.Extensions;
using OneOf;
using Remora.Commands.Attributes;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Caching;
using Remora.Discord.Caching.Abstractions.Services;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = System.Drawing.Color;

namespace Olwe.Remora.Commands.General;

[PrimaryConstructor]
public partial class InfoCommandsAliases : BaseCommandGroup
{
    private readonly InfoCommands _commands;
    private readonly ICommandContext _context;
    private InteractionContext? InteractionContext => _context as InteractionContext;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IDiscordRestUserAPI _userApi;
    
    [Command("userinfo", "uinfo", "ui")]
    [ExcludeFromSlashCommands]
    [Description("Shows information about a user.")]
    public async Task<IResult> UserInfo(IUser? user = null) => await _commands.UserOrMemberInfoAsync(user);

    [Command("emojiinfo", "einfo", "ri")]
    [ExcludeFromSlashCommands]
    [Description("Shows information about an emoji.")]
    public async Task<IResult> UserInfo(IPartialEmoji emoji) => await _commands.GetEmojiInfoAsync(emoji);

    [Command("roleinfo", "rinfo", "ri")]
    [ExcludeFromSlashCommands]
    [Description("Shows information about a role.")]
    public async Task<IResult> UserInfo(IRole role) => await _commands.GetRoleInfoAsync(role);

    [Command("serverinfo", "sinfo", "si")]
    [ExcludeFromSlashCommands]
    [RequireContext(ChannelContext.Guild)]
    [Description("Shows information about the server.")]
    public async Task<IResult> ServerInfo() => await _commands.ServerInfo();

    [Command("User Info")]
    [CommandType(ApplicationCommandType.User)]
    [Ephemeral]
    [RequireContext(ChannelContext.Guild)]
    public async Task<IResult> UserInfoContext()
    {
        if (!InteractionContext!.Data.AsT0.TargetID.IsDefined(out var id))
            return Result.FromError(new NotFoundError("User not found"));
        await _commands.UnCacheUserAsync(id);
        var user = await _userApi.GetUserAsync(id);
        if (!user.IsSuccess)
            return Result.FromError(user);
        var member = await _guildApi.GetGuildMemberAsync(InteractionContext.GuildID.Value, id);
        if (!member.IsSuccess)
            return Result.FromError(member);

        return await _commands.GetMemberInfoAsync((member.Entity as GuildMember)! with
        {
            User = new Optional<IUser>(user.Entity)
        });
    }
}

[Category("General")]
[Group("info")]
[PrimaryConstructor]
public partial class InfoCommands : BaseCommandGroup
{
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IDiscordRestUserAPI _userApi;
    private readonly ICacheProvider _cacheProvider;
    private readonly IDiscordRestEmojiAPI _emojiApi;
    private readonly ICommandContext _context;
    private InteractionContext? InteractionContext => _context as InteractionContext;

    [Command("user")]
    [Ephemeral]
    [Description("Get information about a user")]
    public async Task<IResult> UserOrMemberInfoAsync(IUser? user = null)
    {
        user ??= _context.User;

        await UnCacheUserAsync(user.ID);

        var restUser = await _userApi.GetUserAsync(user.ID);
        if (!_context.GuildID.IsDefined(out var guildId))
            return await GetUserInfoAsync(restUser.Entity);

        var guildMember = await _guildApi.GetGuildMemberAsync(guildId, user.ID);
        if (guildMember.IsSuccess)
            return await GetMemberInfoAsync((guildMember.Entity as GuildMember)! with
            {
                User = new Optional<IUser>(restUser.Entity)
            });

        return await GetUserInfoAsync(restUser.Entity);
    }

    public async Task<IResult> GetMemberInfoAsync(IGuildMember member)
    {
        var roleResult = await _guildApi.GetGuildRolesAsync(_context.GuildID.Value);

        if (!roleResult.IsDefined(out var roleList))
            return roleResult;

        var roles = roleList.ToDictionary(r => r.ID, r => r);

        var user = member.User.Value;

        var avatar = CDN.GetUserAvatarUrl(user, imageSize: 4096);

        if (!avatar.IsSuccess)
            avatar = CDN.GetDefaultUserAvatarUrl(user, imageSize: 4096);

        var bannerUrl = CDN.GetUserBannerUrl(user, imageSize: 4096);

        var bannerImage = default(Stream);

        if (!bannerUrl.IsSuccess)
            bannerImage = !user.AccentColour.IsDefined(out var accent)
                ? default
                : await GenerateBannerColorImageAsync(accent.Value);

        var embed = new Embed
        {
            Title = user.ToDiscordTag(),
            Thumbnail = new EmbedThumbnail(avatar.Entity.ToString()),
            Colour = Color.DodgerBlue,
            Image = new EmbedImage(bannerUrl.IsSuccess ? bannerUrl.Entity.ToString() : "attachment://banner.png"),
            Fields = new EmbedField[]
            {
                new("Account Created", user.ID.Timestamp.ToTimestamp(), true),
                new("Joined", member.JoinedAt.ToTimestamp(), true),
                new("Flags",
                    user.PublicFlags.IsDefined(out var flags)
                        ? (int) flags is 0
                            ? "None"
                            : flags.ToString().Split(' ').Join("\n").Humanize(LetterCasing.Title)
                        : "None", true),
                new("Roles",
                    string.Join(",\n",
                        member.Roles.Append(roles[_context.GuildID.Value].ID).OrderByDescending(r => roles[r].Position)
                            .Select(x => $"<@&{x}>"))),
            }
        };

        if (InteractionContext is null)
        {
            return await _channelApi.CreateMessageAsync
            (
                _context.ChannelID,
                embeds: new[] {embed},
                attachments: bannerUrl.IsSuccess || bannerImage is null
                    ? default(Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>)
                    : new[] {OneOf<FileData, IPartialAttachment>.FromT0(new FileData("banner.png", bannerImage))}
            );
        }

        return await _interactionApi.EditOriginalInteractionResponseAsync
        (
            InteractionContext.ApplicationID,
            InteractionContext.Token,
            embeds: new[] {embed},
            attachments: bannerUrl.IsSuccess ||
                         bannerImage is null
                ? default(Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>)
                : new[] {OneOf<FileData, IPartialAttachment>.FromT0(new FileData("banner.png", bannerImage))}
        );
    }

    private async Task<IResult> GetUserInfoAsync(IUser? user = null)
    {
        user ??= _context.User;

        var avatar = CDN.GetUserAvatarUrl(user, imageSize: 4096);

        if (!avatar.IsSuccess)
            avatar = CDN.GetDefaultUserAvatarUrl(user, imageSize: 4096);

        var bannerUrl = CDN.GetUserBannerUrl(user, imageSize: 4096);

        var bannerImage = default(Stream);

        if (!bannerUrl.IsSuccess)
            bannerImage = !user.AccentColour.IsDefined(out var accent)
                ? default
                : await GenerateBannerColorImageAsync(accent.Value);

        var embed = new Embed
        {
            Title = user.ToDiscordTag(),
            Thumbnail = new EmbedThumbnail(avatar.Entity.ToString()),
            Colour = Color.DodgerBlue,
            Image = new EmbedImage(bannerUrl.IsSuccess ? bannerUrl.Entity.ToString() : "attachment://banner.png"),
            Fields = new EmbedField[]
            {
                new("Account Created", user.ID.Timestamp.ToTimestamp(), true),
                new("Flags", user.PublicFlags.IsDefined(out var flags)
                        ? flags is 0
                            ? "None"
                            : flags.Humanize(LetterCasing.Title)
                        : "None",
                    true),
            }
        };

        if (InteractionContext is null)
        {
            return await _channelApi.CreateMessageAsync
            (
                _context.ChannelID,
                embeds: new[] {embed},
                attachments: bannerUrl.IsSuccess || bannerImage is null
                    ? default(Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>)
                    : new[] {OneOf<FileData, IPartialAttachment>.FromT0(new("banner.png", bannerImage))}
            );
        }

        return await _interactionApi.EditOriginalInteractionResponseAsync
        (
            InteractionContext.ApplicationID,
            InteractionContext.Token,
            embeds: new[] {embed},
            attachments: bannerUrl.IsSuccess || bannerImage is null
                ? default(Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>)
                : new[] {OneOf<FileData, IPartialAttachment>.FromT0(new("banner.png", bannerImage))}
        );
    }

    [Command("role")]
    [RequireContext(ChannelContext.Guild)]
    [Ephemeral]
    [Description("Get information about a role!")]
    public async Task<IResult> GetRoleInfoAsync(IRole role)
    {
        var roleResult = await _guildApi.GetGuildRolesAsync(_context.GuildID.Value);

        if (!roleResult.IsDefined(out var roles))
            return roleResult;

        var highestRole = roles.Max(r => r.Position);

        var hierarchyResult = await GetRoleHierarchyStringAsync(role);

        if (!hierarchyResult.IsDefined(out var hierarchy))
            return hierarchyResult;


        var permissions = role
            .Permissions
            .GetPermissions()
            .Select(p => p.Humanize(LetterCasing.Title))
            .OrderBy(p => p[0])
            .ThenBy(p => p.Length)
            .Select(p => $"`{p}`")
            .Chunk(4)
            .Select(p => p.Aggregate((c, n) => n.Length > 18 ? $"{c}\n{n}" : $"{c}, {n}"))
            .Join("\n");

        var embed = new Embed
        {
            Title = $"{role.Name}",
            Colour = role.Colour,
            Image = new EmbedImage("attachment://swatch.png"),
            Fields = new EmbedField[]
            {
                new("ID", role.ID.ToString(), true),
                new("Created",
                    $"{role.ID.Timestamp.ToTimestamp(TimestampFormat.LongDate)}\n({role.ID.Timestamp.ToTimestamp()})",
                    true),
                new("Position", $"{role.Position}/{highestRole}", true),
                new("Color", $"#{role.Colour.Name[2..].ToUpper()}", true),
                new("Hierarchy", hierarchy, true),
                new("Mentionable", role.IsMentionable ? "Yes" : "No", true),
                new("Hoisted", role.IsHoisted ? "Yes" : "No", true),
                new("Bot/Managed", role.IsManaged ? "Yes" : "No", true),
                new("Permissions", permissions),
            },
        };

        await using var swatchImage = await GenerateRoleColorSwatchAsync(role.Colour);

        if (InteractionContext is null)
        {
            return await _channelApi.CreateMessageAsync
            (
                _context.ChannelID,
                embeds: new[] {embed},
                attachments: new[] {OneOf<FileData, IPartialAttachment>.FromT0(new("swatch.png", swatchImage))}
            );
        }

        return await _interactionApi.EditOriginalInteractionResponseAsync(InteractionContext.ApplicationID,
            InteractionContext.Token, embeds: new[] {embed},
            attachments: new[] {OneOf<FileData, IPartialAttachment>.FromT0(new("swatch.png", swatchImage))});
    }

    [Command("emoji")]
    [RequireContext(ChannelContext.Guild)]
    [Ephemeral]
    [Description("Get information about an emoji!")]
    public async Task<IResult> GetEmojiInfoAsync(IPartialEmoji emoji)
    {
        if (!emoji.ID.IsDefined(out var emojiId))
        {
            if (InteractionContext is null)
                return await _channelApi.CreateMessageAsync(_context.ChannelID,
                    "This appears to be a unicode emoji. I can't tell you anything about it!");
            return await _interactionApi.EditOriginalInteractionResponseAsync(InteractionContext.ApplicationID,
                InteractionContext.Token, "This appears to be a unicode emoji. I can't tell you anything about it!");
        }

        var emojiResult = await _emojiApi.ListGuildEmojisAsync(_context.GuildID.Value);

        if (!emojiResult.IsDefined(out var emojis))
            return emojiResult;

        var guildEmoji = emojis.FirstOrDefault(e => e.ID == emojiId);

        Embed embed;

        var emojiUrl = CDN.GetEmojiUrl((guildEmoji?.ID ?? emojiId.Value), imageSize: 256);

        if (!emojiUrl.IsDefined(out var url))
            return emojiUrl;

        if (guildEmoji is null)
        {
            embed = new()
            {
                Title =
                    $"Info about {(emoji.Name.IsDefined(out var eName) ? eName : "(This emoji is unnamed. Potential bug?)")}",
                Colour = Color.DodgerBlue,
                Image = new EmbedImage(url.ToString()),
                Fields = new EmbedField[]
                {
                    new("ID", emoji.ID.ToString()),
                    new("Created", emoji.ID.Value.Value.Timestamp.ToTimestamp(TimestampFormat.LongDate))
                }
            };
        }
        else
        {
            var roleLocked = guildEmoji.Roles.IsDefined(out var roles) && roles.Any();

            embed = new()
            {
                Title = $"Emoji info for {guildEmoji.Name ?? "(This emoji is unnamed. Potential bug?)"}",
                Colour = Color.DodgerBlue,
                Image = new EmbedImage(url.ToString()),
                Fields = new EmbedField[]
                {
                    new("ID", emoji.ID.Value.Value.ToString()),
                    new("Created", emoji.ID.Value.Value.Timestamp.ToTimestamp(TimestampFormat.LongDate)),
                    new("Animated", guildEmoji.IsAnimated.IsDefined(out var anim) && anim ? "Yes" : "No"),
                    new("Managed", guildEmoji.IsManaged.IsDefined(out var managed) && managed ? "Yes" : "No"),
                    new("Added By", guildEmoji.User.IsDefined(out var addedBy) ? addedBy.ToDiscordTag() : "Unknown"),
                    new("Role-Locked", roleLocked ? "Yes" : "No"),
                    new("Role-Locked to", roleLocked ? roles!.Select(r => $"<@&{r}>").Join(",\n") : "None")
                }
            };
        }

        if (InteractionContext is null)
            return await _channelApi.CreateMessageAsync(_context.ChannelID, embeds: new[] {embed});

        return await _interactionApi.EditOriginalInteractionResponseAsync(InteractionContext.ApplicationID,
            InteractionContext.Token, embeds: new[] {embed});
    }

    [Command("server")]
    [Ephemeral]
    [RequireContext(ChannelContext.Guild)]
    [Description("Shows information about the server.")]
    public async Task<IResult> ServerInfo()
    {
        var guildResult = await _guildApi.GetGuildAsync(_context.GuildID.Value, true);

        if (!guildResult.IsDefined(out var guild))
            return guildResult;

        var fields = new List<IEmbedField>();

        fields.Add(new EmbedField("Server Icon:",
            CDN.GetGuildIconUrl(guild, imageSize: 4096).IsDefined(out var guildIcon)
                ? $"[Link]({guildIcon})"
                : "Not Set!", true));
        fields.Add(new EmbedField("Invite Splash:",
            CDN.GetGuildSplashUrl(guild, imageSize: 4096).IsDefined(out var guildSplash)
                ? $"[Link]({guildSplash})"
                : "Not Set!", true));
        fields.Add(new EmbedField("Server Banner:",
            CDN.GetGuildBannerUrl(guild, imageSize: 4096).IsDefined(out var guildBanner)
                ? $"[Link]({guildBanner})"
                : "Not Set!", true));

        var memberInformation =
            $"Max: {(guild.MaxMembers.IsDefined(out var maxMembers) ? $"{maxMembers}" : "Unknown")}\n" +
            $"Current\\*: {(guild.ApproximateMemberCount.IsDefined(out var memberCount) ? $"{memberCount}" : "Unknown")}\n" +
            $"Online\\*: {(guild.ApproximatePresenceCount.IsDefined(out var onlineCount) ? $"{onlineCount}" : "Unknown")}";

        fields.Add(new EmbedField("Members:", memberInformation, true));

        var channelsResult = await _guildApi.GetGuildChannelsAsync(_context.GuildID.Value);

        if (!channelsResult.IsDefined(out var channels))
        {
            fields.Add(new EmbedField("Channels:", "Channel information is unavailable. Sorry.", true));
        }
        else
        {
            var channelInfo = channels
                .GroupBy(c => c.Type)
                .Select(gc => $"{gc.Key.Humanize()}: {gc.Count()}")
                .Join("\n");

            fields.Add(new EmbedField("Channels:", $"{channelInfo}\n Total: {channels.Count}", true));
        }

        var tier = guild.PremiumTier switch
        {
            PremiumTier.None => ("(No Level)", 100),
            PremiumTier.Tier1 => ("(Level 1)", 200),
            PremiumTier.Tier2 => ("(Level 2)", 300),
            PremiumTier.Tier3 => ("(Level 3)", 500),
            PremiumTier.Tier4 => throw new InvalidOperationException("Tier 4 doesn't exist."),
            _ => throw new ArgumentOutOfRangeException()
        };

        fields.Add(new EmbedField("Other Info:", $"Emojis: {guild.Emojis.Count}/{tier.Item2}\n " +
                                                 $"Roles: {guild.Roles.Count}\n " +
                                                 $"Boosts: {(guild.PremiumSubscriptionCount.IsDefined(out var boosts) ? $"{boosts}" : "Unknown")} {tier.Item1}\n " +
                                                 $"Progress Bar: {(guild.IsPremiumProgressBarEnabled ? "Yes" : "No")}",
            true));


        fields.Add(new EmbedField("Server Owner:", $"<@{guild.OwnerID}>", true));

        fields.Add(new EmbedField("Server Created:",
            $"{guild.ID.Timestamp.ToTimestamp(TimestampFormat.LongDateTime)} ({guild.ID.Timestamp.ToTimestamp()})"));

        var features = guild.GuildFeatures.Any()
            ? guild.GuildFeatures.Select(f => f.Humanize(LetterCasing.Title)).OrderBy(o => o.Length).Join("\n")
            : "None";

        fields.Add(new EmbedField("Features:", features));

        var embed = new Embed
        {
            Title = $"Information about {guild.Name}:",
            Colour = Color.Gold,
            Fields = fields,
            Thumbnail = guildIcon is null
                ? default(Optional<IEmbedThumbnail>)
                : new EmbedThumbnail(guildIcon.ToString()),
            Image = guildBanner is null ? default(Optional<IEmbedImage>) : new EmbedImage(guildBanner.ToString()),
        };

        if (InteractionContext is null)
            return await _channelApi.CreateMessageAsync(_context.ChannelID, embeds: new[] {embed});


        return await _interactionApi.EditOriginalInteractionResponseAsync(InteractionContext.ApplicationID,
            InteractionContext.Token, embeds: new[] {embed});
    }

    private async Task<Stream> GenerateBannerColorImageAsync(Color bannerColor)
    {
        using var image = new Image<Rgba32>(4096, 512, new Rgba32(bannerColor.R, bannerColor.G, bannerColor.B, 255));

        var stream = new MemoryStream();

        await image.SaveAsPngAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        return stream;
    }

    private async Task<Result<string>> GetRoleHierarchyStringAsync(IRole role)
    {
        var roleResult = await _guildApi.GetGuildRolesAsync(_context.GuildID.Value);

        if (!roleResult.IsDefined(out var roles))
            return Result<string>.FromError(roleResult.Error!);

        roles = roles.OrderBy(r => r.Position).ToArray();

        var roleIDs = roles.OrderBy(r => r.Position).Select(r => r.ID).ToArray();

        var currentIndex = Array.IndexOf(roleIDs, role.ID);

        var sb = new StringBuilder();

        if (currentIndex < roles.Count - 1)
        {
            var next = roles[currentIndex + 1];

            sb.AppendLine($"{next.Mention()}");
            sb.AppendLine("\u200b\t↑");
        }

        sb.AppendLine($"{role.Mention()}");

        if (currentIndex > 0)
        {
            var prev = roles[currentIndex - 1];

            sb.AppendLine("\u200b\t↑");
            sb.AppendLine($"{prev.Mention()}");
        }

        return Result<string>.FromSuccess(sb.ToString());
    }

    public async Task UnCacheUserAsync(Snowflake userId) =>
        await _cacheProvider.EvictAsync(KeyHelpers.CreateUserCacheKey(userId));

    private async Task<Stream> GenerateRoleColorSwatchAsync(Color roleColor)
    {
        using var image = new Image<Rgb24>(600, 200, new Rgb24(roleColor.R, roleColor.G, roleColor.B));

        var stream = new MemoryStream();

        await image.SaveAsPngAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        return stream;
    }
}