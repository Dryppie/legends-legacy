using Application.Common.Interfaces;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Outbox;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Encounters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Microsoft.EntityFrameworkCore;

namespace Services.LL.Essences;

public sealed class CreatureArchiveService : ICreatureArchiveService
{
    private static readonly TimeSpan EssenceFocusCooldown = TimeSpan.FromHours(8);

    private readonly IDbContext _dbContext;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;
    private readonly ICreatureEssenceLootTableRepository _creatureEssenceLootTables;
    private readonly IEssenceCodexCollectionService _codexCollections;
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly IGameEventOutbox? _outbox;

    public CreatureArchiveService(
        IDbContext dbContext,
        IEssenceDefinitionRepository essenceDefinitions,
        ICreatureEssenceLootTableRepository creatureEssenceLootTables,
        IEssenceCodexCollectionService codexCollections,
        IDungeonDefinitions dungeonDefinitions,
        IGameEventOutbox? outbox = null)
    {
        _dbContext = dbContext;
        _essenceDefinitions = essenceDefinitions;
        _creatureEssenceLootTables = creatureEssenceLootTables;
        _codexCollections = codexCollections;
        _dungeonDefinitions = dungeonDefinitions;
        _outbox = outbox;
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
        var locationsByCreatureId = await GetCreatureLocationsAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var lastFocusSetAt = GetLastEssenceFocusSetAt(entries);
        var focusAvailableAt = GetEssenceFocusAvailableAt(lastFocusSetAt);
        var canChangeFocus = CanChangeEssenceFocus(lastFocusSetAt, now);

        var creatures = entries
            .Select(entry =>
            {
                var definitions = (_creatureEssenceLootTables
                    .GetByCreatureId(entry.CreatureDefinitionId)?
                    .Variants
                    .Select(x => _essenceDefinitions.GetById(x.EssenceDefinitionId))
                    .Where(x => x is not null)
                    .Cast<EssenceDefinition>()
                    .DistinctBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList()) ?? [];
                return new CreatureArchiveEntry(
                    entry.CreatureDefinitionId,
                    string.IsNullOrWhiteSpace(entry.CreatureName)
                        ? FormatCreatureName(entry.CreatureDefinitionId)
                        : entry.CreatureName,
                    entry.KillCount,
                    entry.FirstDefeatedAtUtc,
                    entry.LastDefeatedAtUtc,
                    entry.IsEssenceFocus,
                    entry.EssenceFocusSetAtUtc,
                    GetTotalEssenceFocusDurationSeconds(entry, now),
                    GetCurrentEssenceFocusDurationSeconds(entry, now),
                    definitions
                        .Select(definition => new CreatureArchiveEssenceEntry(
                            definition.Id,
                            definition.DisplayName,
                            absorbedIds.Contains(definition.Id),
                            definition.Tags))
                        .ToList(),
                    locationsByCreatureId.GetValueOrDefault(entry.CreatureDefinitionId, []),
                    definitions.SelectMany(x => x.Tags).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            })
            .ToList();

        return new CreatureArchive(creatures, canChangeFocus, focusAvailableAt, lastFocusSetAt);
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<CreatureArchiveLocation>>> GetCreatureLocationsAsync(
        CancellationToken cancellationToken)
    {
        var regions = await _dbContext.Regions
            .AsNoTracking()
            .Include(region => region.Areas)
            .ThenInclude(area => area.Creatures)
            .ToListAsync(cancellationToken);
        var creatureNamesById = await _dbContext.Creatures
            .AsNoTracking()
            .ToDictionaryAsync(creature => creature.Id, creature => creature.Name, cancellationToken);
        var locations = new Dictionary<string, List<CreatureArchiveLocation>>(StringComparer.OrdinalIgnoreCase);

        foreach (var region in regions)
        {
            foreach (var area in region.Areas)
            {
                foreach (var areaCreature in area.Creatures)
                {
                    if (!creatureNamesById.TryGetValue(areaCreature.CreatureId, out var creatureName))
                        continue;

                    AddLocation(
                        locations,
                        CreatureEssenceSource.GetMonsterDefinitionId(creatureName),
                        new CreatureArchiveLocation(region.Id, region.Name, "Area", area.Id, area.Name));
                }
            }
        }

        var regionNamesById = regions.ToDictionary(region => region.Id, region => region.Name);
        foreach (var dungeon in _dungeonDefinitions.GetAll())
        {
            var regionName = regionNamesById.GetValueOrDefault(dungeon.Region, $"Region {dungeon.Region}");
            foreach (var encounterId in dungeon.Rooms
                         .SelectMany(room => room.EncounterIds)
                         .Where(id => !string.IsNullOrWhiteSpace(id))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                AddLocation(
                    locations,
                    DungeonEncounterIdentity.ToMonsterDefinitionId(encounterId),
                    new CreatureArchiveLocation(
                        dungeon.Region,
                        regionName,
                        "Dungeon",
                        DungeonDefinitionIdentity.GetFamilyId(dungeon.Id),
                        DungeonDefinitionIdentity.GetFamilyTitle(dungeon.Name)));
            }
        }

        return locations.ToDictionary(
            item => item.Key,
            item => (IReadOnlyList<CreatureArchiveLocation>)item.Value
                .OrderBy(location => location.RegionId)
                .ThenBy(location => location.SourceType)
                .ThenBy(location => location.SourceName)
                .ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AddLocation(
        IDictionary<string, List<CreatureArchiveLocation>> locations,
        string creatureId,
        CreatureArchiveLocation location)
    {
        if (!locations.TryGetValue(creatureId, out var creatureLocations))
        {
            creatureLocations = [];
            locations[creatureId] = creatureLocations;
        }

        if (creatureLocations.Any(existing =>
                existing.RegionId == location.RegionId &&
                existing.SourceType.Equals(location.SourceType, StringComparison.OrdinalIgnoreCase) &&
                existing.SourceId.Equals(location.SourceId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        creatureLocations.Add(location);
    }

    public async Task<CreatureArchive> SetEssenceFocusAsync(
        Guid characterId,
        string? creatureId,
        CancellationToken cancellationToken)
    {
        var entries = await _dbContext.CharacterCreatureArchiveEntries
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(creatureId))
        {
            return await GetCreatureArchiveAsync(characterId, cancellationToken);
        }

        var focusedEntry = entries.FirstOrDefault(entry =>
            entry.CreatureDefinitionId.Equals(creatureId, StringComparison.OrdinalIgnoreCase));
        if (focusedEntry is null || _creatureEssenceLootTables.GetByCreatureId(focusedEntry.CreatureDefinitionId) is null)
        {
            return await GetCreatureArchiveAsync(characterId, cancellationToken);
        }

        if (focusedEntry.IsEssenceFocus)
        {
            return await GetCreatureArchiveAsync(characterId, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        if (!CanChangeEssenceFocus(GetLastEssenceFocusSetAt(entries), now))
        {
            return await GetCreatureArchiveAsync(characterId, cancellationToken);
        }

        foreach (var entry in entries)
        {
            if (entry.IsEssenceFocus)
            {
                entry.EssenceFocusTotalDurationSeconds += GetCurrentEssenceFocusDurationSeconds(entry, now);
                entry.EssenceFocusSetAtUtc = null;
            }

            entry.IsEssenceFocus = false;
        }

        focusedEntry.IsEssenceFocus = true;
        focusedEntry.EssenceFocusSetAtUtc = now;

        if (_outbox is not null)
        {
            await _outbox.EnqueueAsync(
                GameEventTypes.EssenceFocusSet,
                new EssenceFocusSetPayload(characterId, focusedEntry.CreatureDefinitionId),
                characterId,
                null,
                cancellationToken);
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

    public async Task<string?> GetEssenceFocusCreatureIdAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        await _dbContext.CharacterCreatureArchiveEntries
            .Where(x => x.CharacterId == characterId && x.IsEssenceFocus)
            .Select(x => x.CreatureDefinitionId)
            .FirstOrDefaultAsync(cancellationToken);

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

    private static DateTimeOffset? GetLastEssenceFocusSetAt(IEnumerable<CharacterCreatureArchiveEntry> entries)
    {
        DateTimeOffset? lastFocusSetAt = null;
        foreach (var entry in entries)
        {
            if (entry.EssenceFocusSetAtUtc is null)
            {
                continue;
            }

            if (lastFocusSetAt is null || entry.EssenceFocusSetAtUtc.Value > lastFocusSetAt.Value)
            {
                lastFocusSetAt = entry.EssenceFocusSetAtUtc;
            }
        }

        return lastFocusSetAt;
    }

    private static DateTimeOffset? GetEssenceFocusAvailableAt(DateTimeOffset? lastFocusSetAt) =>
        lastFocusSetAt?.Add(EssenceFocusCooldown);

    private static bool CanChangeEssenceFocus(DateTimeOffset? lastFocusSetAt, DateTimeOffset now) =>
        GetEssenceFocusAvailableAt(lastFocusSetAt) is not { } availableAt || availableAt <= now;

    private static long GetTotalEssenceFocusDurationSeconds(CharacterCreatureArchiveEntry entry, DateTimeOffset now) =>
        entry.EssenceFocusTotalDurationSeconds + GetCurrentEssenceFocusDurationSeconds(entry, now);

    private static long GetCurrentEssenceFocusDurationSeconds(CharacterCreatureArchiveEntry entry, DateTimeOffset now)
    {
        if (!entry.IsEssenceFocus || entry.EssenceFocusSetAtUtc is not { } focusSetAt)
        {
            return 0;
        }

        return Math.Max(0, Convert.ToInt64(Math.Floor((now - focusSetAt).TotalSeconds)));
    }

    private sealed record DefeatedCreature(string CreatureId, string Name);
}
