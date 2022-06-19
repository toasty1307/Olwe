using MediatR;
using Microsoft.EntityFrameworkCore;
using Olwe.Data.Entities.Guild;
using Remora.Rest.Core;

namespace Olwe.Data.MediatR;

public record GetOrCreateModConfigRequest(Snowflake GuildId) : IRequest<GuildModConfigEntity>;

[PrimaryConstructor]
internal partial class GetOrCreateModConfigHandler : IRequestHandler<GetOrCreateModConfigRequest, GuildModConfigEntity>
{
    private readonly OlweContext _context;
    
    public async Task<GuildModConfigEntity> Handle(GetOrCreateModConfigRequest request, CancellationToken cancellationToken)
    {
        if (request.GuildId.Value == 0)
            return new GuildModConfigEntity();
        
        var configEntity = await
            _context
                .ModConfigs
                .FirstOrDefaultAsync(c => c.GuildId == request.GuildId, cancellationToken: cancellationToken);

        if (configEntity is not null) return configEntity;

        configEntity = new GuildModConfigEntity
        {
            GuildId = request.GuildId,
        };

        _context.Add(configEntity);
        await _context.SaveChangesAsync(cancellationToken);
        return configEntity;
    }
}