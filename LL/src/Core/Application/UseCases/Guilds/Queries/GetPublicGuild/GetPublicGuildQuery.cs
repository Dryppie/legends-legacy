using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Guilds.Dtos.Responses;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Guilds.Queries.GetPublicGuild;

public record GetPublicGuildQuery(Guid GuildId) : IQuery<GuildPublicDto?>;

public sealed class GetPublicGuildQueryHandler(IGuildService guildService, IMapper mapper)
    : IRequestHandler<GetPublicGuildQuery, GuildPublicDto?>
{
    public async Task<GuildPublicDto?> Handle(GetPublicGuildQuery request, CancellationToken cancellationToken)
    {
        var guild = await guildService.GetPublicGuildAsync(request.GuildId, cancellationToken);
        return mapper.Map<GuildPublicDto?>(guild);
    }
}
