using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Application.UseCases.Colosseum.Dtos;
using Application.UseCases.Colosseum.Events;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Colosseum.Commands.StartArenaBattle;
public record StartArenaBattleCommand(Guid CharacterId, string EnemyId) : ICommand<Response<StartArenaBattleResponseDto>>;
public class StartArenaBattleCommandHandler : IRequestHandler<StartArenaBattleCommand, Response<StartArenaBattleResponseDto>>
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

    public async Task<Response<StartArenaBattleResponseDto>> Handle(StartArenaBattleCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.EnemyId, out var enemyId))
            return Response<StartArenaBattleResponseDto>.Fail("Enemy is not valid.");

        var result = await _colosseumService.StartArenaBattle(request.CharacterId, enemyId, cancellationToken);
        if (result == null)
            return Response<StartArenaBattleResponseDto>.Fail("Failed to start arena battle.");

        await _publisher.Publish(new ArenaBattleCompletedEvent(request.CharacterId, enemyId, result.CombatResult.Outcome), cancellationToken);

        return Response<StartArenaBattleResponseDto>.Success(new StartArenaBattleResponseDto
        {
            Battle = _mapper.Map<CombatResultDto>(result.CombatResult),
            ArenaTicketStatus = _mapper.Map<ArenaTicketStatusDto>(result.ArenaTicketStatus)
        });
    }
}
