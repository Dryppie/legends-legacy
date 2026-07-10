using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Microsoft.EntityFrameworkCore;

namespace Services.LL.Essences;

public sealed class CreatureArchiveService : ICreatureArchiveService
{
    private readonly IDbContext _dbContext;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;
    private readonly IEssenceCodexCollectionService _codexCollections;

    public CreatureArchiveService(
        IDbContext dbContext,
        IEssenceDefinitionRepository essenceDefinitions,
        IEssenceCodexCollectionService codexCollections)
    {
        _dbContext = dbContext;
        _essenceDefinitions = essenceDefinitions;
        _codexCollections = codexCollections;
    }

    public async Task RecordDefeatedCreaturesAsync(
        Guid characterId,
        IReadOnlyCollection<Creature> creatures,
        DateTimeOffset defeatedAtUtc,
        CancellationToken cancellationToken)
    {
        if (creatures.Count == 0)
        {
            return;
        }

        var defeated = creatures
            .Select(creature => new DefeatedCreature(
                CreatureEssenceSource.GetMonsterDefinitionId(creature),
                creature.Name.Trim()))
            .Where(creature => !string.IsNullOrWhiteSpace(creature.CreatureId))
            .GroupBy(creature => creature.CreatureId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                CreatureId = group.Key,
                Name = group
                    .Select(x => x.Name)
                    .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty,
                Count = group.Count()
            })
            .ToList();

        if (defeated.Count == 0)
        {
            return;
        }

        var creatureIds = defeated.Select(x => x.CreatureId).ToArray();
        var existing = await _dbContext.CharacterCreatureArchiveEntries
            .Where(x => x.CharacterId == characterId && creatureIds.Contains(x.CreatureDefinitionId))
            .ToListAsync(cancellationToken);

        foreach (var creature in defeated)
        {
            var entry = existing.FirstOrDefault(x =>
                x.CreatureDefinitionId.Equals(creature.CreatureId, StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                _dbContext.CharacterCreatureArchiveEntries.Add(new CharacterCreatureArchiveEntry
                {
                    Id = Guid.NewGuid(),
                    CharacterId = characterId,
                    CreatureDefinitionId = creature.CreatureId,
                    CreatureName = string.IsNullOrWhiteSpace(creature.Name)
                        ? FormatCreatureName(creature.CreatureId)
                        : creature.Name,
                    KillCount = creature.Count,
                    FirstDefeatedAtUtc = defeatedAtUtc,
                    LastDefeatedAtUtc = defeatedAtUtc
                });
                continue;
            }

            entry.KillCount += creature.Count;
            if (string.IsNullOrWhiteSpace(entry.CreatureName) && !string.IsNullOrWhiteSpace(creature.Name))
            {
                entry.CreatureName = creature.Name;
            }

            entry.LastDefeatedAtUtc = defeatedAtUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CreatureArchive> GetCreatureArchiveAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.CharacterCreatureArchiveEntries
            .Where(x => x.CharacterId == characterId)
            .OrderByDescending(x => x.KillCount)
            .ThenBy(x => x.CreatureName)
            .ToListAsync(cancellationToken);
        var absorbedIds = await GetAbsorbedEssenceDefinitionIdsAsync(characterId, cancellationToken);

        var creatures = entries
            .Select(entry =>
            {
                var definition = _essenceDefinitions.GetByMonsterId(entry.CreatureDefinitionId);
                return new CreatureArchiveEntry(
                    entry.CreatureDefinitionId,
                    string.IsNullOrWhiteSpace(entry.CreatureName)
                        ? FormatCreatureName(entry.CreatureDefinitionId)
                        : entry.CreatureName,
                    entry.KillCount,
                    entry.FirstDefeatedAtUtc,
                    entry.LastDefeatedAtUtc,
                    entry.IsEssenceFocus,
                    definition?.Id,
                    definition?.Name,
                    definition is not null && absorbedIds.Contains(definition.Id),
                    definition?.Tags ?? []);
            })
            .ToList();

        return new CreatureArchive(creatures);
    }

    public async Task<CreatureArchive> SetEssenceFocusAsync(
        Guid characterId,
        string? creatureId,
        CancellationToken cancellationToken)
    {
        var entries = await _dbContext.CharacterCreatureArchiveEntries
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            entry.IsEssenceFocus = false;
        }

        if (!string.IsNullOrWhiteSpace(creatureId))
        {
            var focusedEntry = entries.FirstOrDefault(entry =>
                entry.CreatureDefinitionId.Equals(creatureId, StringComparison.OrdinalIgnoreCase));

            if (focusedEntry is not null && _essenceDefinitions.GetByMonsterId(focusedEntry.CreatureDefinitionId) is not null)
            {
                focusedEntry.IsEssenceFocus = true;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetCreatureArchiveAsync(characterId, cancellationToken);
    }

    public async Task<bool> IsEssenceFocusAsync(Guid characterId, string creatureId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(creatureId))
        {
            return false;
        }

        return await _dbContext.CharacterCreatureArchiveEntries
            .AnyAsync(
                x =>
                    x.CharacterId == characterId &&
                    x.IsEssenceFocus &&
                    x.CreatureDefinitionId == creatureId,
                cancellationToken);
    }

    public async Task<EssenceCodex> GetEssenceCodexAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var entries = await _codexCollections.GetVisibleEntriesAsync(characterId, cancellationToken);
        return new EssenceCodex(entries);
    }

    private async Task<HashSet<string>> GetAbsorbedEssenceDefinitionIdsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var ids = await _dbContext.PlayerEssences
            .Where(x => x.CharacterId == characterId)
            .Select(x => x.EssenceDefinitionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string FormatCreatureName(string creatureId)
    {
        const string monsterPrefix = "monster.";
        var raw = creatureId.StartsWith(monsterPrefix, StringComparison.OrdinalIgnoreCase)
            ? creatureId[monsterPrefix.Length..]
            : creatureId;

        return string.Join(' ', raw
            .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private sealed record DefeatedCreature(string CreatureId, string Name);
}
