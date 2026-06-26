using Application.Common.Interfaces;
using Domain.Models.Colosseum;
using Domain.Models.Entities.Characters;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Colosseum;
public class ColosseumRepository : IColosseumRepository
{
    private readonly IDbContext _context;

    public ColosseumRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<Character?> GetArenaCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _context.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);
    }

    public async Task<(List<Character> Opponents, int MyRating)> GetArenaOpponentsWithRating(Guid characterId, CancellationToken cancellationToken)
    {
        var characters = await _context.Characters
            .Where(c => c.Id == characterId || c.Id != characterId)
            .Select(c => new { c.Id, c.Name, c.ArenaRating, Character = c })
            .ToListAsync(cancellationToken);

        var self = characters.FirstOrDefault(c => c.Id == characterId);
        if (self == null) return ([], 0);

        var opponents = characters
            .Where(c => c.Id != characterId
                        && c.Character.UserId != self.Character.UserId)
            .OrderBy(c => Math.Abs(c.ArenaRating - self.ArenaRating))
            .Take(25)
            .Select(c => c.Character)
            .ToList();

        return (opponents, self.ArenaRating);
    }

    public async Task<List<ColosseumMatchResult>> GetColosseumMatchResults(Guid characterId, CancellationToken cancellationToken)
    {
        var colosseumMatchResults = await _context.ColosseumMatches
            .Where(cm => cm.CharacterAId == characterId || cm.CharacterBId == characterId)
            .OrderByDescending(cm => cm.PlayedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        return colosseumMatchResults;
    }

    public async Task<bool> HasRecentMatchAsync(Guid attackerCharacterId, Guid defenderCharacterId, DateTimeOffset since, CancellationToken cancellationToken)
    {
        return await _context.ColosseumMatches
            .AnyAsync(match =>
                match.CharacterAId == attackerCharacterId &&
                match.CharacterBId == defenderCharacterId &&
                match.PlayedAt >= since,
                cancellationToken);
    }

    public async Task<List<Character>> GetRankings(Guid characterId, CancellationToken cancellationToken)
    {
        var characters = await _context.Characters
            .OrderByDescending(c => c.ArenaRating)
            .ToListAsync(cancellationToken);

        return characters;
    }

    public async Task SaveArenaMatchResult(ColosseumMatchResult arenaMatchResult, CancellationToken cancellationToken)
    {
        await _context.ColosseumMatches.AddAsync(arenaMatchResult, cancellationToken);
    }

    public async Task<ArenaTicketStatus> GetArenaTicketStatusAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var arenaTicketStatus = await _context.ArenaTicketStatus
            .FindAsync([characterId], cancellationToken);

        if (arenaTicketStatus == null) // Create new arena ticket status in case a player has none
        {
            arenaTicketStatus = new ArenaTicketStatus()
            {
                CharacterId = characterId,
                CurrentTickets = 5,
                LastTicketUpdate = DateTimeOffset.UtcNow,
            };
            await CreateArenaTicketStatusAsync(arenaTicketStatus, cancellationToken);
        }

        return arenaTicketStatus;
    }

    private async Task CreateArenaTicketStatusAsync(ArenaTicketStatus arenaTicketStatus, CancellationToken cancellationToken)
    {
        await _context.ArenaTicketStatus.AddAsync(arenaTicketStatus, cancellationToken);
    }

    public void UpdateArenaTicketStatus(ArenaTicketStatus arenaTicketStatus)
    {
        _context.ArenaTicketStatus.Update(arenaTicketStatus);
    }

    public async Task<ArenaDefenseSnapshot?> GetArenaDefenseSnapshotAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _context.ArenaDefenseSnapshots
            .Include(x => x.CharacterSnapshot)
                .ThenInclude(x => x.BaseAttributes)
            .Include(x => x.CharacterSnapshot)
                .ThenInclude(x => x.Equipment)
                    .ThenInclude(x => x.InstanceModifiers)
            .Include(x => x.CharacterSnapshot)
                .ThenInclude(x => x.EquippedEssences)
            .FirstOrDefaultAsync(x => x.CharacterId == characterId, cancellationToken);
    }

    public async Task SaveArenaDefenseSnapshotAsync(ArenaDefenseSnapshot snapshot, CancellationToken cancellationToken)
    {
        var existing = await _context.ArenaDefenseSnapshots
            .FirstOrDefaultAsync(x => x.CharacterId == snapshot.CharacterId, cancellationToken);

        if (existing is null)
        {
            await _context.ArenaDefenseSnapshots.AddAsync(snapshot, cancellationToken);
            return;
        }

        existing.CharacterSnapshotId = snapshot.CharacterSnapshotId;
        existing.LoadoutHash = snapshot.LoadoutHash;
        existing.IsValid = snapshot.IsValid;
        existing.IsOutdated = snapshot.IsOutdated;
        existing.UpdatedAt = snapshot.UpdatedAt;
    }

    public async Task<int> CountChampionMarketPurchasesAsync(Guid characterId, string itemId, DateTimeOffset? since, CancellationToken cancellationToken)
    {
        var query = _context.ChampionMarketPurchases
            .Where(x => x.CharacterId == characterId && x.ItemId == itemId);

        if (since.HasValue)
        {
            query = query.Where(x => x.PurchasedAt >= since.Value);
        }

        return await query.SumAsync(x => x.Quantity, cancellationToken);
    }

    public async Task SaveChampionMarketPurchaseAsync(ChampionMarketPurchase purchase, CancellationToken cancellationToken)
    {
        await _context.ChampionMarketPurchases.AddAsync(purchase, cancellationToken);
    }
}
