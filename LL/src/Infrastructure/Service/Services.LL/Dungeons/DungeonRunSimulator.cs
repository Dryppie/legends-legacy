using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Professions;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Dungeons.Definitions.Encounters;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Professions.Craftings;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using AdminCreatureService = Application.Interfaces.Services.AdminDashboard.ICreatureService;

namespace Services.LL.Dungeons;

public sealed class DungeonRunSimulator : IDungeonRunSimulator
{
    private const int MaximumRuns = 500;
    private static readonly IReadOnlyList<SimulationEquipmentSlot> SimulationEquipmentSlots =
    [
        new("Head", "Head", EquipmentType.Head),
        new("Chest", "Chest", EquipmentType.Chest),
        new("Legs", "Legs", EquipmentType.Legs),
        new("Ring", "Ring", EquipmentType.Ring),
        new("Necklace", "Necklace", EquipmentType.Necklace),
        new("Relic", "Relic", EquipmentType.Relic),
        new("MainHand", "Main hand", EquipmentType.OneHanded),
        new("OffHand", "Off hand", EquipmentType.OffHand)
    ];

    private readonly IDungeonDefinitions _dungeons;
    private readonly IEssenceDefinitionRepository _essences;
    private readonly DungeonRunFactory _runFactory;
    private readonly AdminCreatureService _creatures;
    private readonly IEntityService _entities;
    private readonly ICombatSetupService _combatSetup;
    private readonly ICombatEngineExecutor _combatEngine;
    private readonly ICombatEncounterResultFactory _resultFactory;
    private readonly IDungeonVigorService _vigor;
    private readonly ICraftingDefinitionProvider _craftingDefinitions;
    private readonly CraftingBalanceOptions _craftingBalance;

    public DungeonRunSimulator(
        IDungeonDefinitions dungeons,
        IEssenceDefinitionRepository essences,
        DungeonRunFactory runFactory,
        AdminCreatureService creatures,
        IEntityService entities,
        ICombatSetupService combatSetup,
        ICombatEngineExecutor combatEngine,
        ICombatEncounterResultFactory resultFactory,
        IDungeonVigorService vigor,
        ICraftingDefinitionProvider craftingDefinitions,
        IOptions<CraftingBalanceOptions> craftingBalance)
    {
        _dungeons = dungeons;
        _essences = essences;
        _runFactory = runFactory;
        _creatures = creatures;
        _entities = entities;
        _combatSetup = combatSetup;
        _combatEngine = combatEngine;
        _resultFactory = resultFactory;
        _vigor = vigor;
        _craftingDefinitions = craftingDefinitions;
        _craftingBalance = craftingBalance.Value;
    }

    public DungeonSimulationOptions GetOptions() => new(
        _dungeons.GetAll()
            .OrderBy(dungeon => dungeon.Region)
            .ThenBy(dungeon => GetFamilyId(dungeon.Id), StringComparer.OrdinalIgnoreCase)
            .ThenBy(dungeon => dungeon.Tier)
            .Select(dungeon => new DungeonSimulationDungeonOption(
                dungeon.Id,
                GetFamilyId(dungeon.Id),
                dungeon.Name,
                GetDifficultyName(dungeon.Tier),
                dungeon.Tier,
                dungeon.RecommendedCombatRating))
            .ToList(),
        _essences.GetAll()
            .OrderBy(essence => essence.Name, StringComparer.OrdinalIgnoreCase)
            .Select(essence => new DungeonSimulationEssenceOption(essence.Id, essence.Name))
            .ToList(),
        SimulationEquipmentSlots
            .Select(slot => new DungeonSimulationEquipmentSlotOption(
                slot.Id,
                slot.Name,
                GetEquipmentAttributeBonuses(slot.EquipmentType)
                    .ToDictionary(pair => pair.Key.ToString(), pair => pair.Value)))
            .ToList(),
        Enum.GetValues<Rarity>()
            .Select(rarity => new DungeonSimulationEquipmentRarityOption(
                rarity.ToString(),
                rarity.ToString(),
                GetRarityMultiplier(rarity)))
            .ToList());

    public async Task<DungeonSimulationReport> RunAsync(
        DungeonSimulationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Character);

        var dungeon = _dungeons.GetByKey(request.DungeonDefinitionId);
        var runCount = Math.Clamp(request.RunCount, 1, MaximumRuns);
        var baseSeed = request.RandomSeed == 0 ? 1337 : request.RandomSeed;
        var masteryLevel = Math.Clamp(request.MasteryLevel, 0, 10);
        var routeStrategy = NormalizeRouteStrategy(request.RouteStrategy);
        var character = NormalizeCharacter(request.Character);
        var results = new List<DungeonSimulationRunResult>(runCount);

        for (var runIndex = 0; runIndex < runCount; runIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var seed = unchecked(baseSeed + runIndex * 7919);
            results.Add(await RunSingleAsync(
                dungeon.Id,
                dungeon.Tier,
                character,
                masteryLevel,
                routeStrategy,
                runIndex + 1,
                seed,
                cancellationToken));
        }

        var completed = results.Count(result => result.Completed);
        var simulatedRating = CalculateCombatRating(character);

        return new DungeonSimulationReport(
            dungeon.Id,
            dungeon.Name,
            GetDifficultyName(dungeon.Tier),
            dungeon.Tier,
            dungeon.RecommendedCombatRating,
            simulatedRating,
            runCount,
            completed,
            runCount - completed,
            Math.Round(completed * 100d / runCount, 2),
            Math.Round(results.Average(result => result.FinalVigor), 2),
            Math.Round(results.Average(result => result.RoomsCleared), 2),
            baseSeed,
            routeStrategy,
            results);
    }

    private async Task<DungeonSimulationRunResult> RunSingleAsync(
        string dungeonDefinitionId,
        int dungeonTier,
        DungeonSimulationCharacter character,
        int masteryLevel,
        string routeStrategy,
        int runNumber,
        int seed,
        CancellationToken cancellationToken)
    {
        var run = _runFactory.CreateForSimulation(dungeonDefinitionId, seed);
        run.State.MasteryLevelAtStart = masteryLevel;
        _vigor.RefreshState(run);
        var random = new Random(seed);
        var roomResults = new List<DungeonSimulationRoomResult>();
        var completed = false;
        var outcome = "No route available";
        var roomsCleared = 0;
        var totalCombatTicks = 0;

        while (true)
        {
            var currentNode = run.State.MapNodes.FirstOrDefault(node => node.RoomIndex == run.CurrentRoomIndex);
            if (currentNode is null || currentNode.NextRoomIndexes.Count == 0)
            {
                completed = run.Rooms.FirstOrDefault(room => room.RoomIndex == run.CurrentRoomIndex)?.Type == RoomType.Boss;
                outcome = completed ? "Completed" : outcome;
                break;
            }

            var nextRoomIndex = SelectNextRoom(run, currentNode.NextRoomIndexes, routeStrategy, random);
            run.CurrentRoomIndex = nextRoomIndex;
            var room = run.Rooms.Single(candidate => candidate.RoomIndex == nextRoomIndex);
            var node = run.State.MapNodes.Single(candidate => candidate.RoomIndex == nextRoomIndex);
            var vigorBefore = run.State.Vigor;

            if (room.Type == RoomType.RestSite)
            {
                _vigor.RecoverAtRestSite(run, room);
                roomsCleared++;
                roomResults.Add(new DungeonSimulationRoomResult(
                    room.RoomIndex,
                    node.DisplayName,
                    room.Type.ToString(),
                    "Rested",
                    vigorBefore,
                    run.State.Vigor,
                    run.State.Vigor - vigorBefore,
                    0,
                    0,
                    []));
                continue;
            }

            if (room.Type is not (RoomType.Combat or RoomType.MiniBoss or RoomType.Boss))
            {
                roomsCleared++;
                continue;
            }

            var combatResult = await SimulateCombatAsync(
                run,
                room,
                dungeonTier,
                character,
                cancellationToken);
            totalCombatTicks += combatResult.Duration;
            var damageTaken = combatResult.EntityStats
                .Where(stats => stats.Team.Equals("Friendly", StringComparison.OrdinalIgnoreCase))
                .Sum(stats => Math.Max(0, stats.DamageTaken));

            if (combatResult.Outcome != BattleOutcome.Victory)
            {
                outcome = $"Defeated in {node.DisplayName}";
                roomResults.Add(CreateCombatRoomResult(
                    room,
                    node.DisplayName,
                    combatResult,
                    vigorBefore,
                    run.State.Vigor,
                    damageTaken));
                break;
            }

            _vigor.ApplyCombatToll(run, room, combatResult);
            roomsCleared++;
            roomResults.Add(CreateCombatRoomResult(
                room,
                node.DisplayName,
                combatResult,
                vigorBefore,
                run.State.Vigor,
                damageTaken));

            if (run.State.Vigor <= 0)
            {
                outcome = $"Vigor spent after {node.DisplayName}";
                break;
            }

            if (room.Type == RoomType.Boss)
            {
                completed = true;
                outcome = "Completed";
                break;
            }
        }

        return new DungeonSimulationRunResult(
            runNumber,
            seed,
            completed,
            outcome,
            run.State.Vigor,
            roomsCleared,
            totalCombatTicks,
            roomResults);
    }

    private async Task<CombatResult> SimulateCombatAsync(
        DungeonRun run,
        RoomInstance room,
        int dungeonTier,
        DungeonSimulationCharacter characterConfiguration,
        CancellationToken cancellationToken)
    {
        var character = CreateCharacter(characterConfiguration);
        var player = new CombatEntity(character)
        {
            Equipment = CreateSimulationEquipment(characterConfiguration.Equipment),
            EquippedEssences = characterConfiguration.EssenceIds
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(essenceId => new PlayerEssence
                {
                    Id = Guid.NewGuid(),
                    CharacterId = character.Id,
                    EssenceDefinitionId = essenceId,
                    Level = Math.Max(1, character.Level)
                })
                .ToList(),
            HasEquippedEssenceSnapshot = true
        };

        if (run.State.VigorState == "Exhausted")
        {
            player.TemporaryModifiers.Add(new DungeonAttributeModifier(
                AttributeType.MaxHealth,
                -10,
                ModifierType.Additive));
        }

        var creatureKeys = room.EncounterIds
            .Select(DungeonEncounterIdentity.NormalizeCreatureKey)
            .ToList();
        var creatureIds = await _creatures.GetCreaturesByKey(creatureKeys, cancellationToken);
        var sourceEntities = await _entities.GetEntitiesByIdsForCombatAsync(
            creatureIds.Distinct().ToList(),
            cancellationToken);
        var creaturesById = sourceEntities
            .OfType<Creature>()
            .ToDictionary(creature => creature.Id);
        var creatureEntities = creatureIds
            .Where(creaturesById.ContainsKey)
            .Select(id => creaturesById[id])
            .ToList();

        if (creatureEntities.Count != creatureKeys.Count)
        {
            throw new InvalidOperationException(
                $"Could not resolve every creature for room {room.RoomIndex}. Expected {creatureKeys.Count}, found {creatureEntities.Count}.");
        }

        var hostiles = _combatSetup.CreateCreatureCombatEntities(
            [.. creatureEntities],
            new Area { DifficultyTier = Math.Max(1, dungeonTier) });
        await _combatSetup.PrepareEntitiesForCombat([player, .. hostiles]);

        var slots = new List<CombatParticipantSlot>
        {
            new("simulated-character", character.Id, CombatSide.Friendly)
        };
        var friendly = new CombatRuntimeParticipant(slots[0], character, player);
        player.Id = slots[0].SlotId;
        var hostileParticipants = new List<CombatRuntimeParticipant>();

        for (var index = 0; index < hostiles.Count; index++)
        {
            var slot = new CombatParticipantSlot(
                $"enemy-{index + 1}",
                creatureEntities[index].Id,
                CombatSide.Hostile);
            slots.Add(slot);
            hostiles[index].Id = slot.SlotId;
            hostileParticipants.Add(new CombatRuntimeParticipant(slot, creatureEntities[index], hostiles[index]));
        }

        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Dungeon,
            1,
            DateTimeOffset.UtcNow,
            slots,
            new DungeonEncounterSourceContext(run.Id));
        var runtime = new CombatEncounterRuntime(plan, [friendly], hostileParticipants);
        var result = await _combatEngine.ExecuteAsync(runtime, cancellationToken);
        return _resultFactory.Create(runtime, result).CombatResult;
    }

    private static DungeonSimulationRoomResult CreateCombatRoomResult(
        RoomInstance room,
        string displayName,
        CombatResult combatResult,
        int vigorBefore,
        int vigorAfter,
        int damageTaken) => new(
            room.RoomIndex,
            displayName,
            room.Type.ToString(),
            combatResult.Outcome.ToString(),
            vigorBefore,
            vigorAfter,
            vigorAfter - vigorBefore,
            combatResult.Duration,
            damageTaken,
            room.EncounterIds);

    private static Character CreateCharacter(DungeonSimulationCharacter configuration) => new()
    {
        Id = Guid.NewGuid(),
        Name = configuration.Name,
        Level = configuration.Level,
        BaseAttributes = CreateAttributeDictionary(configuration)
            .Select(pair => new EntityAttribute { AttributeType = pair.Key, Value = pair.Value })
            .ToList()
    };

    private int CalculateCombatRating(DungeonSimulationCharacter character)
    {
        var equipmentModifiers = CreateSimulationEquipment(character.Equipment)
            .SelectMany(item => item.AttributeModifiers)
            .ToList();
        var attributes = AttributeCalculator.CalculateProjectedAttributes(
            CreateAttributeDictionary(character),
            equipmentModifiers);

        return CombatRatingCalculator.Calculate(attributes, character.Level);
    }

    private static Dictionary<AttributeType, float> CreateAttributeDictionary(
        DungeonSimulationCharacter character) => new()
    {
        [AttributeType.MaxHealth] = character.MaxHealth,
        [AttributeType.Power] = character.Power,
        [AttributeType.Fortitude] = character.Fortitude,
        [AttributeType.Spirit] = character.Spirit,
        [AttributeType.Armor] = character.Armor,
        [AttributeType.Resistance] = character.Resistance,
        [AttributeType.Precision] = character.Precision,
        [AttributeType.CritChance] = character.CritChance,
        [AttributeType.CritDamage] = character.CritDamage,
        [AttributeType.AttackSpeed] = character.AttackSpeed,
        [AttributeType.HealthRegeneration] = character.HealthRegeneration
    };

    private static int SelectNextRoom(
        DungeonRun run,
        IReadOnlyList<int> roomIndexes,
        string strategy,
        Random random)
    {
        var candidates = roomIndexes
            .Select(index => run.State.MapNodes.Single(node => node.RoomIndex == index))
            .ToList();

        return strategy switch
        {
            "Safest" => candidates
                .OrderBy(node => node.VigorCostMax)
                .ThenBy(node => node.VigorCostMin)
                .ThenBy(node => node.RoomIndex)
                .First().RoomIndex,
            "Hardest" => candidates
                .OrderByDescending(node => node.VigorCostMax)
                .ThenByDescending(node => node.VigorCostMin)
                .ThenBy(node => node.RoomIndex)
                .First().RoomIndex,
            _ => candidates[random.Next(candidates.Count)].RoomIndex
        };
    }

    private static DungeonSimulationCharacter NormalizeCharacter(
        DungeonSimulationCharacter character) => character with
    {
        Name = string.IsNullOrWhiteSpace(character.Name) ? "Simulated Character" : character.Name.Trim(),
        Level = Math.Clamp(character.Level, 1, 1000),
        MaxHealth = Math.Clamp(character.MaxHealth, 1, 10_000_000),
        Power = Math.Clamp(character.Power, 0, 1_000_000),
        Fortitude = Math.Clamp(character.Fortitude, 0, 1_000_000),
        Spirit = Math.Clamp(character.Spirit, 0, 1_000_000),
        Armor = Math.Clamp(character.Armor, 0, 1_000_000),
        Resistance = Math.Clamp(character.Resistance, 0, 1_000_000),
        Precision = Math.Clamp(character.Precision, 0, 1_000_000),
        CritChance = Math.Clamp(character.CritChance, 0, 100),
        CritDamage = Math.Clamp(character.CritDamage, 0, 1000),
        AttackSpeed = Math.Clamp(character.AttackSpeed, 0, 1000),
        HealthRegeneration = Math.Clamp(character.HealthRegeneration, 0, 1_000_000),
        EssenceIds = (character.EssenceIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList(),
        Equipment = NormalizeEquipment(character.Equipment)
    };

    private static DungeonSimulationEquipment NormalizeEquipment(DungeonSimulationEquipment? equipment)
    {
        var rarity = Enum.TryParse<Rarity>(equipment?.Rarity, true, out var parsedRarity)
            ? parsedRarity
            : Rarity.Common;
        var validSlots = SimulationEquipmentSlots
            .Select(slot => slot.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var equippedSlots = (equipment?.EquippedSlots ?? [])
            .Where(validSlots.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DungeonSimulationEquipment(rarity.ToString(), equippedSlots);
    }

    private List<EquipmentInstance> CreateSimulationEquipment(DungeonSimulationEquipment? configuration)
    {
        var normalized = NormalizeEquipment(configuration);
        var equippedSlotIds = normalized.EquippedSlots.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rarity = Enum.Parse<Rarity>(normalized.Rarity);

        return SimulationEquipmentSlots
            .Where(slot => equippedSlotIds.Contains(slot.Id))
            .Select(slot =>
            {
                var itemBaseId = $"simulation.{slot.Id.ToLowerInvariant()}";
                var itemBase = new EquipmentBase
                {
                    Id = itemBaseId,
                    Name = $"Simulated {slot.Name}",
                    EquipmentType = slot.EquipmentType,
                    Rarity = Rarity.Common,
                    AttributeModifiers = GetEquipmentAttributeBonuses(slot.EquipmentType)
                        .Select(pair => new ItemAttributeModifier(pair.Key, pair.Value)
                        {
                            ItemBaseId = itemBaseId
                        })
                        .ToList()
                };

                return new EquipmentInstance
                {
                    Id = Guid.NewGuid(),
                    ItemBaseId = itemBaseId,
                    ItemBase = itemBase,
                    Rarity = rarity,
                    Tier = 1
                };
            })
            .ToList();
    }

    private IReadOnlyDictionary<AttributeType, float> GetEquipmentAttributeBonuses(EquipmentType equipmentType)
    {
        var recipe = _craftingDefinitions.GetRecipes().FirstOrDefault(candidate =>
            candidate.RecipeType == RecipeType.Base &&
            candidate.OutputItemType == equipmentType);
        if (recipe is null)
            throw new InvalidOperationException($"No base crafting recipe exists for simulated {equipmentType} equipment.");

        var profile = recipe.BaseStatProfileOverride ?? recipe.BaseStatProfile;
        var budget = _craftingBalance.GetTierPowerBudget(1) *
                     _craftingBalance.GetSlotBudgetWeight(equipmentType);

        return profile
            .Where(pair => pair.Value > 0)
            .ToDictionary(
                pair => pair.Key,
                pair => (float)Math.Max(1, Math.Round(budget * pair.Value)));
    }

    private static float GetRarityMultiplier(Rarity rarity) =>
        new EquipmentInstance { Rarity = rarity }.Boost;

    private static string NormalizeRouteStrategy(string? strategy) =>
        strategy?.Trim().ToLowerInvariant() switch
        {
            "safest" => "Safest",
            "hardest" => "Hardest",
            _ => "Random"
        };

    private static string GetFamilyId(string dungeonId) => dungeonId
        .Replace("_iii", string.Empty, StringComparison.OrdinalIgnoreCase)
        .Replace("_ii", string.Empty, StringComparison.OrdinalIgnoreCase);

    private static string GetDifficultyName(int tier) => tier switch
    {
        1 => "Novice",
        2 => "Veteran",
        _ => "Champion"
    };

    private sealed record SimulationEquipmentSlot(
        string Id,
        string Name,
        EquipmentType EquipmentType);
}
