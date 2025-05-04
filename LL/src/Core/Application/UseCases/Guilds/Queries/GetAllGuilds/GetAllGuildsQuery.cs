using Application.Interfaces.Services.LL;
using Application.UseCases.Guilds.Dtos.Responses;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Guilds.Queries.GetAllGuilds;
public record GetAllGuildsQuery() : IRequest<List<GuildSimpleDto>>;

public class GetAllGuildsQueryHandler : IRequestHandler<GetAllGuildsQuery, List<GuildSimpleDto>>
{
    private readonly IGuildService _guildService;
    private readonly IMapper _mapper;

    public GetAllGuildsQueryHandler(IGuildService guildService, IMapper mapper)
    {
        _guildService = guildService;
        _mapper = mapper;
    }

    public async Task<List<GuildSimpleDto>> Handle(GetAllGuildsQuery request, CancellationToken cancellationToken)
    {
        var guilds = await _guildService.GetAllGuildsAsync(cancellationToken);

        return _mapper.Map<List<GuildSimpleDto>>(guilds);
    }
}