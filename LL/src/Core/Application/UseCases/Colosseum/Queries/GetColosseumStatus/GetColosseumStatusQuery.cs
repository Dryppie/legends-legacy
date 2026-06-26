using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using Application.UseCases.Colosseum.Dtos;
using Application.UseCases.Colosseum.Models;
using AutoMapper;
using Domain.Models.Colosseum;
using MediatR;

namespace Application.UseCases.Colosseum.Queries.GetColosseumStatus;

public record GetColosseumStatusQuery(Guid CharacterId) : IQuery<ColosseumStatusDto>;

public sealed class GetColosseumStatusQueryHandler : IRequestHandler<GetColosseumStatusQuery, ColosseumStatusDto>
{
    private readonly IColosseumService _colosseumService;
    private readonly IMapper _mapper;

    public GetColosseumStatusQueryHandler(IColosseumService colosseumService, IMapper mapper)
    {
        _colosseumService = colosseumService;
        _mapper = mapper;
    }

    public async Task<ColosseumStatusDto> Handle(GetColosseumStatusQuery request, CancellationToken cancellationToken)
    {
        var character = await _colosseumService.GetArenaCharacterAsync(request.CharacterId, cancellationToken)
            ?? throw new InvalidOperationException("Character was not found.");
        var tickets = await _colosseumService.GetArenaTicketStatusAsync(request.CharacterId, cancellationToken);
        var defense = await _colosseumService.GetArenaDefenseSnapshotAsync(request.CharacterId, cancellationToken);
        var rankProgress = ArenaRank.GetProgress(character.ArenaRating);

        return _mapper.Map<ColosseumStatusDto>(new ColosseumStatusModel(
            character.ArenaRating,
            character.ArenaLifetimeHighestRating,
            rankProgress,
            character.ArenaGlory,
            tickets.CurrentTickets,
            tickets.MaxTickets,
            tickets.CurrentTickets >= tickets.MaxTickets ? null : tickets.LastTicketUpdate.AddHours(3),
            character.ArenaCurrentAttackWinStreak,
            character.ArenaBestAttackWinStreak,
            !HasReceivedFirstWinBonusToday(character.ArenaLastFirstWinBonusAt),
            ArenaRewards.DailyFirstWinGlory,
            new ArenaRecordModel(
                character.ArenaAttackWins,
                character.ArenaAttackDraws,
                character.ArenaAttackLosses),
            new ArenaRecordModel(
                character.ArenaDefenseWins,
                character.ArenaDefenseDraws,
                character.ArenaDefenseLosses),
            new ArenaDefenseStatusModel(
                defense is not null,
                defense?.IsValid == true,
                defense?.IsOutdated == true,
                defense?.UpdatedAt,
                defense?.LoadoutHash)));
    }

    private static bool HasReceivedFirstWinBonusToday(DateTimeOffset? lastFirstWinBonusAt)
    {
        return lastFirstWinBonusAt?.UtcDateTime.Date == DateTimeOffset.UtcNow.UtcDateTime.Date;
    }
}
