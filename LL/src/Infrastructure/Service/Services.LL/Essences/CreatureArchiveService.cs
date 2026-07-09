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

    public CreatureArchiveService(
        IDbContext dbContext,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        _dbContext = dbContext;
        _essenceDefinitions = essenceDefinitions;
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
                    definition?.Id,
                    definition?.Name,
                    definition is not null && absorbedIds.Contains(definition.Id),
                    definition?.Tags ?? []);
            })
            .ToList();

        return new CreatureArchive(creatures);
    }

    public async Task<EssenceCodex> GetEssenceCodexAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var absorbedDefinitions = await GetAbsorbedEssenceDefinitionsAsync(characterId, cancellationToken);
        var uniqueCount = absorbedDefinitions.Count;
        var beastCount = absorbedDefinitions.Count(def => HasTag(def, "Beast"));
        var nativeRegionCount = absorbedDefinitions
            .Select(def => def.NativeRegion)
            .Distinct()
            .Count();
        var evolvedCount = await _dbContext.PlayerEssences
            .Where(x => x.CharacterId == characterId && x.IsEvolved)
            .CountAsync(cancellationToken);
        var activeAttunedCount = await _dbContext.EssenceLoadoutSlots
            .Where(slot =>
                slot.PlayerEssenceId != null &&
                slot.EssenceLoadout.CharacterId == characterId &&
                slot.EssenceLoadout.IsActive)
            .CountAsync(cancellationToken);

        return new EssenceCodex(
        [
            CreateEntry(
                "codex.first-echo",
                "First Echo",
                "Absorb your first Essence into the Soul Archive.",
                "Unlocks Codex tracking for Essence collection milestones.",
                uniqueCount,
                1,
                "Collection"),
            CreateEntry(
                "codex.beast-studies-i",
                "Beast Studies I",
                "Archive three Beast-tagged Essences.",
                "Marks Beast creatures as a studied family in the Codex.",
                beastCount,
                3,
                "Creature Families"),
            CreateEntry(
                "codex.regional-survey-i",
                "Regional Survey I",
                "Archive Essences from three different native regions.",
                "Shows regional Essence collection breadth in the Codex.",
                nativeRegionCount,
                3,
                "Regions"),
            CreateEntry(
                "codex.attunement-practice",
                "Attunement Practice",
                "Place an archived Essence into an active loadout.",
                "Records that your Soul Archive has been used in an active combat setup.",
                activeAttunedCount,
                1,
                "Loadouts"),
            CreateEntry(
                "codex.evolution-notes-i",
                "Evolution Notes I",
                "Evolve one archived Essence.",
                "Records evolved Essence discoveries in the Codex.",
                evolvedCount,
                1,
                "Progression")
        ]);
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

    private async Task<IReadOnlyList<Domain.Models.Essences.Definitions.EssenceDefinition>> GetAbsorbedEssenceDefinitionsAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var absorbedIds = await GetAbsorbedEssenceDefinitionIdsAsync(characterId, cancellationToken);
        return _essenceDefinitions.GetAll()
            .Where(def => absorbedIds.Contains(def.Id))
            .ToList();
    }

    private static EssenceCodexEntry CreateEntry(
        string id,
        string title,
        string description,
        string benefitText,
        int current,
        int required,
        string category) =>
        new(
            id,
            title,
            description,
            benefitText,
            Math.Clamp(current, 0, required),
            required,
            current >= required,
            category);

    private static bool HasTag(Domain.Models.Essences.Definitions.EssenceDefinition definition, string tag)
    {
        return definition.Tags.Any(definitionTag =>
            definitionTag.Equals(tag, StringComparison.OrdinalIgnoreCase) ||
            definitionTag.EndsWith($".{tag}", StringComparison.OrdinalIgnoreCase));
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
