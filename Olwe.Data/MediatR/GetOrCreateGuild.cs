using MediatR;
using Microsoft.EntityFrameworkCore;
using Olwe.Data.Entities.Guild;
using Remora.Rest.Core;

namespace Olwe.Data.MediatR;

public record GetOrCreateGuildRequest(Snowflake GuildId) : IRequest<GuildEntity>;

[PrimaryConstructor]
internal partial class GetOrCreateGuildHandler : IRequestHandler<GetOrCreateGuildRequest, GuildEntity>
{
    private readonly OlweContext _db;

    public async Task<GuildEntity> Handle(GetOrCreateGuildRequest request, CancellationToken cancellationToken)
    {
        var guildEntity = await _db
            .Guilds
            .FirstOrDefaultAsync(
                guild => guild.Id == request.GuildId,
                cancellationToken: cancellationToken
            );

        if (guildEntity is not null)
            return guildEntity;

        guildEntity = new GuildEntity
        {
            Id = request.GuildId
        };

        _db.Guilds.Add(guildEntity);

        await _db.SaveChangesAsync(cancellationToken);
        return guildEntity;
    }
}