using Application.Interfaces.Services.LL.Colosseum;
using Application.UseCases.Colosseum.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Queries.GetArenaTickets;
public record GetArenaTicketsQuery(Guid CharacterId) : IRequest<ArenaTicketStatusDto>;

public class GetArenaTicketsQueryHandler : IRequestHandler<GetArenaTicketsQuery, ArenaTicketStatusDto>
{

    private readonly IColosseumService _colosseumService;
    private readonly IMapper _mapper;

    public GetArenaTicketsQueryHandler(IColosseumService colosseumService, IMapper mapper)
    {
        _colosseumService = colosseumService;
        _mapper = mapper;
    }

    public async Task<ArenaTicketStatusDto> Handle(GetArenaTicketsQuery request, CancellationToken cancellationToken)
    {
        var arenaTicketStatus = await _colosseumService.GetArenaTicketStatusAsync(request.CharacterId, cancellationToken);

        return _mapper.Map<ArenaTicketStatusDto>(arenaTicketStatus);
    }
}