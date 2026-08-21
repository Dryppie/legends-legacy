using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using Application.UseCases.Colosseum.Dtos;
using Application.UseCases.Colosseum;
using Application.UseCases.Colosseum.Events;
using Application.UseCases.Colosseum.Models;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Colosseum;
using MediatR;

namespace Application.UseCases.Colosseum.Commands.StartArenaBattle;
public record StartArenaBattleCommand(Guid CharacterId, Guid OpponentId) : ICommand<Response<StartArenaBattleResponseDto>>;
public class StartArenaBattleCommandHandler : IRequestHandler<StartArenaBattleCommand, Response<StartArenaBattleResponseDto>>
{
    private static readonly TimeSpan SameOpponentCooldown = TimeSpan.FromMinutes(2);

    private readonly IColosseumService _colosseumService;
    private readonly IColosseumRepository _colosseumRepository;
    private readonly IMapper _mapper;
    private readonly IPublisher _publisher;
    private readonly ColosseumStateResponseFactory _responses;

    public StartArenaBattleCommandHandler(
        IColosseumService colosseumService,
        IColosseumRepository colosseumRepository,
        IMapper mapper,
        IPublisher publisher,
        ColosseumStateResponseFactory responses)
    {
        _colosseumService = colosseumService;
        _colosseumRepository = colosseumRepository;
        _mapper = mapper;
        _publisher = publisher;
        _responses = responses;
    }

    public async Task<Response<StartArenaBattleResponseDto>> Handle(StartArenaBattleCommand request, CancellationToken cancellationToken)
    {
        if (request.OpponentId == Guid.Empty)
            return Response<StartArenaBattleResponseDto>.Fail("Opponent is not valid.");

        if (await _colosseumRepository.HasRecentMatchAsync(
                request.CharacterId,
                request.OpponentId,
                DateTimeOffset.UtcNow.Subtract(SameOpponentCooldown),
                cancellationToken))
        {
            return Response<StartArenaBattleResponseDto>.Fail("You can challenge the same opponent again after 2 minutes.");
        }

        var result = await _colosseumService.StartArenaBattle(request.CharacterId, request.OpponentId, cancellationToken);
        if (result == null)
            return Response<StartArenaBattleResponseDto>.Fail("Failed to start arena battle.");

        await _publisher.Publish(new ArenaBattleCompletedEvent(
            request.CharacterId,
            request.OpponentId,
            result.CombatResult.Outcome,
            result.MatchResult.CharacterARatingBefore,
            result.MatchResult.CharacterARatingAfter,
            result.MatchResult.CharacterBRatingBefore,
            result.MatchResult.CharacterBRatingAfter), cancellationToken);

        var response = _mapper.Map<StartArenaBattleResponseDto>(
            new StartArenaBattleResponseModel(
                result.BattleId,
                result.CombatResult,
                result.CombatResult,
                new ArenaBattleOutcomeModel(
                    result.CombatResult.Outcome.ToString(),
                    result.MatchResult.CharacterAId,
                    result.MatchResult.CharacterBId,
                    result.MatchResult.WinnerId,
                    result.MatchResult.PlayedAt),
                result.ArenaTicketStatus,
                new ArenaRewardModel(
                    result.GloryEarned,
                    result.BaseGloryEarned,
                    result.DailyFirstWinBonus,
                    0,
                    result.DefenderGloryEarned),
                new ArenaRatingChangeModel(
                    result.MatchResult.CharacterARatingBefore,
                    result.MatchResult.CharacterARatingAfter,
                    result.MatchResult.CharacterARatingDelta),
                new ArenaRatingChangeModel(
                    result.MatchResult.CharacterBRatingBefore,
                    result.MatchResult.CharacterBRatingAfter,
                    result.MatchResult.CharacterBRatingDelta),
                new ArenaRankChangeModel(
                    result.AttackerRankBefore,
                    result.AttackerRankAfter,
                    result.AttackerRankBefore.CurrentTierId != result.AttackerRankAfter.CurrentTierId),
                new ArenaStreakChangeModel(
                    result.AttackStreakBefore,
                    result.AttackStreakAfter,
                    0),
                result.Opponent));
        response.State = await _responses.CreateAsync(
            request.CharacterId,
            cancellationToken,
            result.MatchResult);

        return Response<StartArenaBattleResponseDto>.Success(response);
    }
}
