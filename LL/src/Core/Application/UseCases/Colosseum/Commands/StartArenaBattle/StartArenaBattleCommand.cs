using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Application.UseCases.Colosseum.Events;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Colosseum.Commands.StartArenaBattle;
public record StartArenaBattleCommand(Guid CharacterId, string EnemyId) : ICommand<Response<CombatResultDto>>;
public class StartArenaBattleCommandHandler : IRequestHandler<StartArenaBattleCommand, Response<CombatResultDto>>
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

    public async Task<Response<CombatResultDto>> Handle(StartArenaBattleCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.EnemyId, out var enemyId)) return Response<CombatResultDto>.Fail("Enemy is not valid.");

        var combatResult = await _colosseumService.StartArenaBattle(request.CharacterId, enemyId, cancellationToken);
        if (combatResult == null) return Response<CombatResultDto>.Fail("Failed to start arena battle.");

        await _publisher.Publish(new ArenaBattleCompletedEvent(request.CharacterId, enemyId, combatResult.Outcome), cancellationToken);

        return Response<CombatResultDto>.Success(_mapper.Map<CombatResultDto>(combatResult));
    }
}