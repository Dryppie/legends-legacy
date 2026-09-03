using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Entities.Creatures.Templates.Enums;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Seeds.Seeding;

public static class SeedCreatures
{
    private static readonly string CreatureCatalogPath = Path.Combine("world", "creatures.json");
    private static readonly string RegionCatalogPath = Path.Combine("world", "regions.json");
    private const float FloatTolerance = 0.0001f;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task SeedCreaturesData(this LLDbContext context)
    {
        var content = await LoadContentAsync();
        await UpsertCreaturesAsync(context, content.Creatures);
        await UpsertRegionsAsync(context, content.Regions, createMissingRegions: true);
    }

    public static async Task<bool> EnsureRemainingRegionOneIdleAreas(LLDbContext context)
        => await EnsureAuthoredIdleRegions(context);

    public static async Task<bool> EnsureAuthoredIdleRegions(LLDbContext context)
    {
        var content = await LoadContentAsync();
        var seedRegionNames = content.Regions
            .Select(region => region.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasSeededRegion = await context.Regions
            .AnyAsync(region => seedRegionNames.Contains(region.Name));

        if (!hasSeededRegion)
        {
            return false;
        }

        var changed = false;
        changed |= await UpsertCreaturesAsync(context, content.Creatures);
        changed |= await UpsertRegionsAsync(context, content.Regions, createMissingRegions: true);

        return changed;
    }

    private static async Task<CreatureSeedContent> LoadContentAsync()
    {
        var creatureCatalog = await LoadJsonAsync<CreatureSeedCatalog>(CreatureCatalogPath);
        var regionCatalog = await LoadJsonAsync<RegionSeedCatalog>(RegionCatalogPath);

        Validate(creatureCatalog, regionCatalog);

        return new CreatureSeedContent(creatureCatalog.Creatures, regionCatalog.Regions);
    }

    private static async Task<T> LoadJsonAsync<T>(string relativePath)
    {
        var path = FindDataFile(relativePath);
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Could not deserialize Data/{relativePath}.");
    }

    private static string FindDataFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(directory.FullName, "Data", relativePath),
                Path.Combine(directory.FullName, "src", "API", "API.LL", "Data", relativePath),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL", "Data", relativePath)
            })
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate Data/{relativePath} from '{AppContext.BaseDirectory}'.");
    }

    private static void Validate(CreatureSeedCatalog creatureCatalog, RegionSeedCatalog regionCatalog)
    {
        if (creatureCatalog.Creatures.Count == 0)
        {
            throw new InvalidOperationException("Data/world/creatures.json must contain at least one creature.");
        }

        var duplicateCreatureIds = creatureCatalog.Creatures
            .GroupBy(creature => creature.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateCreatureIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Data/world/creatures.json contains duplicate creature ids: {string.Join(", ", duplicateCreatureIds)}.");
        }

        var creatureIds = creatureCatalog.Creatures
            .Select(creature => creature.Id)
            .ToHashSet();
        var areaIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var region in regionCatalog.Regions)
        {
            if (string.IsNullOrWhiteSpace(region.Name))
            {
                throw new InvalidOperationException("Data/world/regions.json contains a region without a name.");
            }

            foreach (var area in region.Areas)
            {
                if (!areaIds.Add(area.Id))
                {
                    throw new InvalidOperationException($"Data/world/regions.json contains duplicate area id '{area.Id}'.");
                }

                if (area.SpawnProbabilities.Count == 0)
                {
                    throw new InvalidOperationException($"Area '{area.Id}' must define spawn probabilities.");
                }

                if (area.RequiredTowerFloor is < 1)
                {
                    throw new InvalidOperationException($"Area '{area.Id}' has an invalid required Tower floor.");
                }

                foreach (var creature in area.Creatures)
                {
                    if (!creatureIds.Contains(creature.CreatureId))
                    {
                        throw new InvalidOperationException(
                            $"Area '{area.Id}' references unknown creature '{creature.CreatureId}'.");
                    }

                    if (creature.WeightedSpawnRate < 0)
                    {
                        throw new InvalidOperationException(
                            $"Area '{area.Id}' has a negative spawn weight for creature '{creature.CreatureId}'.");
                    }
                }
            }
        }
    }

    private static async Task<bool> UpsertCreaturesAsync(LLDbContext context, IReadOnlyCollection<CreatureSeed> seeds)
    {
        var creatureIds = seeds.Select(seed => seed.Id).ToArray();
        var existingCreatures = await context.Creatures
            .Include(creature => creature.StatOverrides)
            .Where(creature => creatureIds.Contains(creature.Id))
            .ToDictionaryAsync(creature => creature.Id);
        var changed = false;

        foreach (var seed in seeds)
        {
            if (!existingCreatures.TryGetValue(seed.Id, out var existing))
            {
                await context.Creatures.AddAsync(CreateCreature(seed));
                changed = true;
                continue;
            }

            changed |= SetIfChanged(existing.Name, seed.Name, value => existing.Name = value);
            changed |= SetIfChanged(existing.ImagePath, seed.ImagePath, value => existing.ImagePath = value);
            changed |= SetIfChanged(existing.Archetype, seed.Archetype, value => existing.Archetype = value);
            changed |= SetIfChanged(existing.DamageProfile, seed.DamageProfile, value => existing.DamageProfile = value);
            changed |= SetIfChanged(existing.DefenseProfile, seed.DefenseProfile, value => existing.DefenseProfile = value);
            changed |= SetIfChanged(existing.RewardTableId, seed.RewardTableId, value => existing.RewardTableId = value);
            changed |= SetIfChanged(existing.BaseLevel, seed.BaseLevel, value => existing.BaseLevel = value);
            changed |= SetIfChanged(existing.Tier, seed.Tier, value => existing.Tier = value);
            changed |= SyncStatOverrides(context, existing, seed.StatOverrides);
        }

        return changed;
    }

    private static Creature CreateCreature(CreatureSeed seed) =>
        new()
        {
            Id = seed.Id,
            Name = seed.Name,
            ImagePath = seed.ImagePath,
            Archetype = seed.Archetype,
            DamageProfile = seed.DamageProfile,
            DefenseProfile = seed.DefenseProfile,
            RewardTableId = seed.RewardTableId,
            BaseLevel = seed.BaseLevel,
            Tier = seed.Tier,
            StatOverrides = seed.StatOverrides.Select(CreateStatOverride).ToList()
        };

    private static bool SyncStatOverrides(
        LLDbContext context,
        Creature creature,
        IReadOnlyCollection<CreatureStatOverrideSeed> desiredOverrides)
    {
        var changed = false;
        var desiredByAttribute = desiredOverrides.ToDictionary(statOverride => statOverride.AttributeType);

        foreach (var existing in creature.StatOverrides.ToList())
        {
            if (!desiredByAttribute.Remove(existing.AttributeType, out var desired))
            {
                creature.StatOverrides.Remove(existing);
                context.Remove(existing);
                changed = true;
                continue;
            }

            changed |= SetIfChanged(existing.Multiplier, desired.Multiplier, value => existing.Multiplier = value);
            changed |= SetIfChanged(existing.Additive, desired.Additive, value => existing.Additive = value);
        }

        foreach (var desired in desiredByAttribute.Values)
        {
            creature.StatOverrides.Add(CreateStatOverride(desired));
            changed = true;
        }

        return changed;
    }

    private static StatOverride CreateStatOverride(CreatureStatOverrideSeed seed) =>
        new()
        {
            Id = seed.Id == Guid.Empty ? Guid.NewGuid() : seed.Id,
            AttributeType = seed.AttributeType,
            Multiplier = seed.Multiplier,
            Additive = seed.Additive
        };

    private static async Task<bool> UpsertRegionsAsync(
        LLDbContext context,
        IReadOnlyCollection<RegionSeed> seeds,
        bool createMissingRegions)
    {
        var seedRegionNames = seeds.Select(seed => seed.Name).ToArray();
        var existingRegions = await context.Regions
            .Include(region => region.Areas)
            .ThenInclude(area => area.Creatures)
            .Include(region => region.Areas)
            .Where(region => seedRegionNames.Contains(region.Name))
            .ToDictionaryAsync(region => region.Name, StringComparer.OrdinalIgnoreCase);
        var changed = false;

        foreach (var seed in seeds)
        {
            if (!existingRegions.TryGetValue(seed.Name, out var region))
            {
                if (!createMissingRegions)
                {
                    continue;
                }

                await context.Regions.AddAsync(new Region
                {
                    Name = seed.Name,
                    Areas = seed.Areas.Select(CreateArea).ToList()
                });
                changed = true;
                continue;
            }

            foreach (var areaSeed in seed.Areas)
            {
                var area = region.Areas.FirstOrDefault(existingArea =>
                    existingArea.Id.Equals(areaSeed.Id, StringComparison.OrdinalIgnoreCase));
                if (area is null)
                {
                    region.Areas.Add(CreateArea(areaSeed));
                    changed = true;
                    continue;
                }

                changed |= SyncArea(context, area, areaSeed);
            }
        }

        return changed;
    }

    private static Area CreateArea(AreaSeed seed) =>
        new()
        {
            Id = seed.Id,
            Name = seed.Name,
            LevelRequirement = seed.LevelRequirement,
            DifficultyTier = seed.DifficultyTier,
            RequiredActiveQuestId = seed.RequiredActiveQuestId,
            RequiredCompletedQuestId = seed.RequiredCompletedQuestId,
            RequiredTowerFloor = seed.RequiredTowerFloor,
            HideWhenLocked = seed.HideWhenLocked,
            SpawnProbabilities = seed.SpawnProbabilities.ToList(),
            Creatures = seed.Creatures
                .Select(creature => new AreaCreature
                {
                    AreaId = seed.Id,
                    CreatureId = creature.CreatureId,
                    WeightedSpawnRate = creature.WeightedSpawnRate
                })
                .ToList(),
        };

    private static bool SyncArea(LLDbContext context, Area area, AreaSeed seed)
    {
        var changed = false;
        changed |= SetIfChanged(area.Name, seed.Name, value => area.Name = value);
        changed |= SetIfChanged(area.LevelRequirement, seed.LevelRequirement, value => area.LevelRequirement = value);
        changed |= SetIfChanged(area.DifficultyTier, seed.DifficultyTier, value => area.DifficultyTier = value);
        changed |= SetIfChanged(area.RequiredActiveQuestId, seed.RequiredActiveQuestId, value => area.RequiredActiveQuestId = value);
        changed |= SetIfChanged(area.RequiredCompletedQuestId, seed.RequiredCompletedQuestId, value => area.RequiredCompletedQuestId = value);
        changed |= SetIfChanged(area.RequiredTowerFloor, seed.RequiredTowerFloor, value => area.RequiredTowerFloor = value);
        changed |= SetIfChanged(area.HideWhenLocked, seed.HideWhenLocked, value => area.HideWhenLocked = value);

        if (!FloatsEqual(area.SpawnProbabilities, seed.SpawnProbabilities))
        {
            area.SpawnProbabilities = seed.SpawnProbabilities.ToList();
            changed = true;
        }

        changed |= SyncAreaCreatures(context, area, seed.Creatures);

        return changed;
    }

    private static bool SyncAreaCreatures(
        LLDbContext context,
        Area area,
        IReadOnlyCollection<AreaCreatureSeed> desiredCreatures)
    {
        var changed = false;
        var desiredByCreatureId = desiredCreatures.ToDictionary(creature => creature.CreatureId);

        foreach (var existing in area.Creatures.ToList())
        {
            if (!desiredByCreatureId.Remove(existing.CreatureId, out var desired))
            {
                area.Creatures.Remove(existing);
                context.Remove(existing);
                changed = true;
                continue;
            }

            changed |= SetIfChanged(existing.AreaId, area.Id, value => existing.AreaId = value);
            if (Math.Abs(existing.WeightedSpawnRate - desired.WeightedSpawnRate) > FloatTolerance)
            {
                existing.WeightedSpawnRate = desired.WeightedSpawnRate;
                changed = true;
            }
        }

        foreach (var desired in desiredByCreatureId.Values)
        {
            area.Creatures.Add(new AreaCreature
            {
                AreaId = area.Id,
                CreatureId = desired.CreatureId,
                WeightedSpawnRate = desired.WeightedSpawnRate
            });
            changed = true;
        }

        return changed;
    }

    private static bool FloatsEqual(IReadOnlyList<float> existing, IReadOnlyList<float> desired)
    {
        if (existing.Count != desired.Count)
        {
            return false;
        }

        for (var i = 0; i < existing.Count; i++)
        {
            if (Math.Abs(existing[i] - desired[i]) > FloatTolerance)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SetIfChanged<T>(T existing, T updated, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(existing, updated))
        {
            return false;
        }

        setter(updated);
        return true;
    }

    private sealed record CreatureSeedContent(
        IReadOnlyCollection<CreatureSeed> Creatures,
        IReadOnlyCollection<RegionSeed> Regions);

    private sealed class CreatureSeedCatalog
    {
        public List<CreatureSeed> Creatures { get; set; } = [];
    }

    private sealed class CreatureSeed
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public CreatureArchetype Archetype { get; set; } = CreatureArchetype.Balanced;
        public DamageProfile DamageProfile { get; set; } = DamageProfile.Hybrid;
        public DefenseProfile DefenseProfile { get; set; } = DefenseProfile.Balanced;
        public string? RewardTableId { get; set; }
        public int BaseLevel { get; set; } = 1;
        public int Tier { get; set; } = 1;
        public List<CreatureStatOverrideSeed> StatOverrides { get; set; } = [];
    }

    private sealed class CreatureStatOverrideSeed
    {
        public Guid Id { get; set; }
        public AttributeType AttributeType { get; set; }
        public float? Multiplier { get; set; }
        public float? Additive { get; set; }
    }

    private sealed class RegionSeedCatalog
    {
        public List<RegionSeed> Regions { get; set; } = [];
    }

    private sealed class RegionSeed
    {
        public string Name { get; set; } = string.Empty;
        public List<AreaSeed> Areas { get; set; } = [];
    }

    private sealed class AreaSeed
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int LevelRequirement { get; set; }
        public int DifficultyTier { get; set; }
        public string? RequiredActiveQuestId { get; set; }
        public string? RequiredCompletedQuestId { get; set; }
        public int? RequiredTowerFloor { get; set; }
        public bool HideWhenLocked { get; set; }
        public List<float> SpawnProbabilities { get; set; } = [];
        public List<AreaCreatureSeed> Creatures { get; set; } = [];
    }

    private sealed class AreaCreatureSeed
    {
        public Guid CreatureId { get; set; }
        public float WeightedSpawnRate { get; set; }
    }

}
