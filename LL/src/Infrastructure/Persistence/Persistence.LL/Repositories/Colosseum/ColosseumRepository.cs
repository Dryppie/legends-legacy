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
        var character = await _context.Characters
            .Include(c => c.ArenaProfile)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character is not null && character.ArenaProfile is null)
        {
            character.ArenaProfile = new CharacterArenaProfile { CharacterId = character.Id };
            await _context.CharacterArenaProfiles.AddAsync(character.ArenaProfile, cancellationToken);
        }

        return character;
    }

    public async Task<(List<Character> Opponents, int MyRating)> GetArenaOpponentsWithRating(Guid characterId, CancellationToken cancellationToken)
    {
        var characters = await _context.Characters
            .Include(c => c.ArenaProfile)
            .Where(c => c.Id == characterId || c.Id != characterId)
            .Select(c => new { c.Id, c.Name, c.ArenaProfile.Rating, Character = c })
            .ToListAsync(cancellationToken);

        var self = characters.FirstOrDefault(c => c.Id == characterId);
        if (self == null) return ([], 0);

        var opponents = characters
            .Where(c => c.Id != characterId
                        && c.Character.UserId != self.Character.UserId)
            .OrderBy(c => Math.Abs(c.Rating - self.Rating))
            .Take(25)
            .Select(c => c.Character)
            .ToList();

        return (opponents, self.Rating);
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

    public async Task<IReadOnlyDictionary<Guid, DateTimeOffset>> GetRecentAttackerMatchTimesAsync(
        Guid attackerCharacterId,
        IReadOnlyCollection<Guid> defenderCharacterIds,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        if (defenderCharacterIds.Count == 0)
        {
            return new Dictionary<Guid, DateTimeOffset>();
        }

        return await _context.ColosseumMatches
            .Where(match =>
                match.CharacterAId == attackerCharacterId &&
                defenderCharacterIds.Contains(match.CharacterBId) &&
                match.PlayedAt >= since)
            .GroupBy(match => match.CharacterBId)
            .Select(group => new
            {
                DefenderId = group.Key,
                LastPlayedAt = group.Max(match => match.PlayedAt)
            })
            .ToDictionaryAsync(
                match => match.DefenderId,
                match => match.LastPlayedAt,
                cancellationToken);
    }

    public async Task<List<Character>> GetRankings(Guid characterId, CancellationToken cancellationToken)
    {
        var characters = await _context.Characters
            .Include(c => c.ArenaProfile)
            .OrderByDescending(c => c.ArenaProfile.Rating)
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

    public async Task<IReadOnlyList<ChampionMarketPurchase>> GetChampionMarketPurchasesByItemIdsAsync(
        IReadOnlyCollection<string> itemIds,
        CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
        {
            return [];
        }

        var ids = itemIds.ToArray();
        return await _context.ChampionMarketPurchases
            .Where(x => ids.Contains(x.ItemId))
            .OrderBy(x => x.PurchasedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetAccountIdsForCharactersAsync(
        IReadOnlyCollection<Guid> characterIds,
        CancellationToken cancellationToken)
    {
        if (characterIds.Count == 0)
        {
            return new Dictionary<Guid, Guid>();
        }

        var ids = characterIds.ToArray();
        return await _context.Characters
            .Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.UserId })
            .ToDictionaryAsync(x => x.Id, x => x.UserId, cancellationToken);
    }
}
