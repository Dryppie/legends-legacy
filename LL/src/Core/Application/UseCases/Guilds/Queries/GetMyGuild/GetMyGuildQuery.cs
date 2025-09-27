using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Guilds.Dtos.Responses;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Guilds.Queries.GetMyGuild;
public record GetMyGuildQuery(Guid CharacterId) : IQuery<GuildDto?>;
public class GetMyGuildQueryHandler : IRequestHandler<GetMyGuildQuery, GuildDto?>
{
    private readonly IGuildService _guildService;
    private readonly IMapper _mapper;

    public GetMyGuildQueryHandler(IGuildService guildService, IMapper mapper)
    {
        _guildService = guildService;
        _mapper = mapper;
    }

    public async Task<GuildDto?> Handle(GetMyGuildQuery request, CancellationToken cancellationToken)
    {
        var guild = await _guildService.GetMyGuildAsync(request.CharacterId, cancellationToken);

        return _mapper.Map<GuildDto?>(guild);
    }
}