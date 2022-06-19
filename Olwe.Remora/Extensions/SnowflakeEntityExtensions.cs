using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace Olwe.Remora.Extensions;

public static class SnowflakeEntityExtensions
{
    private enum MentionType
    {
        User,
        Role,
        Channel
    }

    public static string Mention(this IUser user)
        => Mention(user.ID, MentionType.User);

    public static string Mention(this IGuildMember member)
        => member.User.IsDefined(out var user)
            ? Mention(user.ID, MentionType.User)
            : throw new InvalidOperationException("The member object does not contain a user.");

    public static string Mention(this IRole role)
        => Mention(role.ID, MentionType.Role);

    public static string Mention(this IChannel channel)
        => Mention(channel.ID, MentionType.Channel);

    private static string Mention(Snowflake id, MentionType type)
        => type switch
        {
            MentionType.Channel => $"<#{id}>",
            MentionType.Role => $"<@&{id}>",
            MentionType.User => $"<@{id}>",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type,
                "Mention type must be user, role, or channel.")
        };

    public static string ToDiscordTag(this IUser user)
        => $"{user.Username}#{user.Discriminator:0000}";

    public static string ToDiscordTag(this IGuildMember member)
        => member.User.IsDefined(out var user)
            ? user.ToDiscordTag()
            : throw new InvalidOperationException("The member object does not contain a user.");
}