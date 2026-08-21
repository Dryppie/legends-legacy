using Application.Interfaces.Services.LL.Colosseum;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Colosseum.Dtos;
using Application.UseCases.Colosseum.Models;
using Application.UseCases.Leaderboards.Dtos;
using AutoMapper;
using Domain.Models.Colosseum;

namespace Application.UseCases.Colosseum;

public sealed class ColosseumStateResponseFactory(
    IColosseumService colosseum,
    IMapper mapper)
{
    public async Task<ColosseumStateSnapshotDto> CreateAsync(
        Guid characterId,
        CancellationToken cancellationToken,
        ColosseumMatchResult? latestMatch = null)
    {
        var character = await colosseum.GetArenaCharacterAsync(characterId, cancellationToken)
            ?? throw new InvalidOperationException("Character was not found.");
        var tickets = await colosseum.GetArenaTicketStatusAsync(characterId, cancellationToken);
        var defense = await colosseum.GetArenaDefenseSnapshotAsync(characterId, cancellationToken);
        var opponents = await colosseum.GetArenaOpponents(characterId, cancellationToken);
        var rankings = await colosseum.GetRankings(characterId, cancellationToken);
        var previousMatches = await colosseum.GetColosseumMatchResults(characterId, cancellationToken);
        if (latestMatch is not null && previousMatches.All(match => match.Id != latestMatch.Id))
        {
            previousMatches.Insert(0, latestMatch);
        }
        var arena = character.ArenaProfile;

        var status = new ColosseumStatusModel(
            arena.Rating,
            arena.LifetimeHighestRating,
            ArenaRank.GetProgress(arena.Rating),
            arena.Glory,
            tickets.CurrentTickets,
            tickets.MaxTickets,
            tickets.CurrentTickets >= tickets.MaxTickets
                ? null
                : tickets.LastTicketUpdate.AddHours(3),
            arena.CurrentAttackWinStreak,
            arena.BestAttackWinStreak,
            arena.LastFirstWinBonusAt?.UtcDateTime.Date != DateTimeOffset.UtcNow.UtcDateTime.Date,
            ArenaRewards.DailyFirstWinGlory,
            new ArenaRecordModel(arena.AttackWins, arena.AttackDraws, arena.AttackLosses),
            new ArenaRecordModel(arena.DefenseWins, arena.DefenseDraws, arena.DefenseLosses),
            new ArenaDefenseStatusModel(
                defense is not null,
                defense?.IsValid == true,
                defense?.IsOutdated == true,
                defense?.UpdatedAt,
                defense?.LoadoutHash));

        return new ColosseumStateSnapshotDto
        {
            Character = mapper.Map<CharacterDto>(character),
            Status = mapper.Map<ColosseumStatusDto>(status),
            Opponents = mapper.Map<List<ArenaOpponentPreviewDto>>(opponents),
            Rankings = mapper.Map<List<LeaderboardEntryDto>>(rankings),
            PreviousMatches = mapper.Map<List<ColosseumMatchResultDto>>(previousMatches)
        };
    }
}
