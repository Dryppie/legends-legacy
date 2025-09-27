using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Guilds.Dtos.Responses;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Guilds.Queries.GetMyInvites;
public record GetMyInvitesQuery(Guid CharacterId) : IQuery<List<GuildInviteDto>>;
public class GetMyInvitesQueryHandler : IRequestHandler<GetMyInvitesQuery, List<GuildInviteDto>>
{
    private readonly IGuildService _guildService;
    private readonly IMapper _mapper;

    public GetMyInvitesQueryHandler(IGuildService guildService, IMapper mapper)
    {
        _guildService = guildService;
        _mapper = mapper;
    }

    public async Task<List<GuildInviteDto>> Handle(GetMyInvitesQuery request, CancellationToken cancellationToken)
    {
        var invites = await _guildService.GetMyInvitesAsync(request.CharacterId, cancellationToken);

        return _mapper.Map<List<GuildInviteDto>>(invites);
    }
}