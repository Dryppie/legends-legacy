using Application.Interfaces.Services.LL;
using Application.UseCases.CharacterActions.Dtos.CombatDtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Commands.StartArenaBattle;
public record StartArenaBattleCommand(Guid CharacterId, Guid EnemyId) : IRequest<CombatResultDto>;
public class StartArenaBattleCommandHandler : IRequestHandler<StartArenaBattleCommand, CombatResultDto>
{
    private readonly IColosseumService _colosseumService;
    private readonly IMapper _mapper;

    public StartArenaBattleCommandHandler(IColosseumService colosseumService, IMapper mapper)
    {
        _colosseumService = colosseumService;
        _mapper = mapper;
    }

    public async Task<CombatResultDto> Handle(StartArenaBattleCommand request, CancellationToken cancellationToken)
    {
        var combatResult = await _colosseumService.StartArenaBattle(request.CharacterId, request.EnemyId, cancellationToken);

        return _mapper.Map<CombatResultDto>(combatResult);
    }
}