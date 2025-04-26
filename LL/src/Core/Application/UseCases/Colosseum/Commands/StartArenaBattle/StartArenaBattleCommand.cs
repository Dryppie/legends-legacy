using Application.Interfaces.Services.LL;
using Application.UseCases.CharacterActions.Dtos.CombatDtos;
using Application.UseCases.Colosseum.Events;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Commands.StartArenaBattle;
public record StartArenaBattleCommand(Guid CharacterId, Guid EnemyId) : IRequest<CombatResultDto>;
public class StartArenaBattleCommandHandler : IRequestHandler<StartArenaBattleCommand, CombatResultDto>
{
    private readonly IColosseumService _colosseumService;
    private readonly IMapper _mapper;
    private readonly IPublisher _publisher;

    public StartArenaBattleCommandHandler(IColosseumService colosseumService, IMapper mapper, IPublisher publisher)
    {
        _colosseumService = colosseumService;
        _mapper = mapper;
        _publisher = publisher;
    }

    public async Task<CombatResultDto> Handle(StartArenaBattleCommand request, CancellationToken cancellationToken)
    {
        var combatResult = await _colosseumService.StartArenaBattle(request.CharacterId, request.EnemyId, cancellationToken);

        await _publisher.Publish(new ArenaBattleCompletedEvent(request.CharacterId, request.EnemyId, combatResult.Outcome), cancellationToken);

        return _mapper.Map<CombatResultDto>(combatResult);
    }
}