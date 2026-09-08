using System.Text.Json;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Entities.Creatures;
using Services.LL.Essences;
using Services.LL.Items;
using Services.LL.PowerRatings;
using Services.LL.Regions;

namespace BalanceHarness;

/// <summary>Explicit file-only composition. No application host or persisted accounts are loaded.</summary>
public sealed class OfflineContent
{
    public static IReadOnlyList<string> Files { get; } = Array.AsReadOnly(new[]
    {
        "combat/abilities.json", "combat/statuses.json", "combat/summons.json",
        "combat/creature-abilities.json", "essences/essences.json",
        "world/creature-essence-loot-tables.json", "world/creatures.json", "world/regions.json",
        "progression/region-combat-balance.json", "equipment/equipment-starters.v1.json",
        "equipment/equipment-named.v1.json", "equipment/equipment-styles.v1.json",
        "equipment/equipment-sets.v1.json", "items/items.json"
    });

    private readonly string _root;
    private readonly IConfiguration _configuration = new ConfigurationBuilder().Build();
    private readonly JsonCreatureEssenceLootTableRepository _creatureEssences;
    private readonly JsonCreatureAbilityDefinitionProvider _creatureAbilities;
    private readonly RegionCreatureScalingProvider _scaling;
    private readonly JsonAbilityCatalogProvider _abilities;
    private readonly ThreatAndTankingOptions _threat;

    public StarterEquipmentCatalog Equipment { get; }
    public JsonEssenceDefinitionRepository Essences { get; }

    public OfflineContent(string root, ThreatAndTankingOptions threat)
    {
        _root = root;
        _threat = threat;
        Essences = new(_configuration, root, HarnessJson.Options, new EssenceDefinitionValidator());
        _creatureEssences = new(_configuration, root, HarnessJson.Options, Essences);
        _creatureAbilities = new(_configuration, root, HarnessJson.Options);
        _scaling = new(_configuration, root, HarnessJson.Options);
        _abilities = new(_configuration, root, HarnessJson.Options, threat);
        Equipment = JsonStarterEquipmentCatalog.Load(Path.Combine(root, "Data", "equipment", "equipment-starters.v1.json"));
    }

    public CanonicalEquipmentBuild CreateStarter()
    {
        var resolver = new SelectedEssenceResolver(Essences);
        var references = new EquipmentReferenceBuildFactory(Equipment, Essences, resolver);
        return new CanonicalEquipmentBuildFactory(Equipment, references, resolver, Essences)
            .CreateTutorialStarterBuild();
    }

    public IdleBattleInput CreateInput(IdleScenario scenario, int seed,
        ThreatAndTankingOptions threat, double cadenceSeconds)
    {
        if (scenario.SchemaVersion is not (1 or 2) || string.IsNullOrWhiteSpace(scenario.Id))
            throw new InvalidDataException("Expected a named version-1 or version-2 idle scenario.");
        ValidateCreatureSelection(scenario);
        FixtureCharacter character;
        if (scenario.Build is not null && scenario.CharacterProfile == scenario.Build.Id)
            character = FixtureCharacter.From(CreateBuild(scenario.Build));
        else if (scenario.Build is null && scenario.CharacterProfile == CanonicalEquipmentBuildFactory.TutorialStarterBuildId)
            character = FixtureCharacter.From(CreateStarter());
        else throw new InvalidDataException("Unknown character profile or mismatched build ID.");
        var areas = HarnessJson.Read<JsonElement>(Path.Combine(_root, "Data", "world", "regions.json"))
            .GetProperty("regions").EnumerateArray().SelectMany(x => x.GetProperty("areas").EnumerateArray());
        var area = areas.SingleOrDefault(x => x.GetProperty("id").GetString() == scenario.AreaId);
        if (area.ValueKind == JsonValueKind.Undefined)
            throw new InvalidDataException($"Unknown area '{scenario.AreaId}'.");
        var creatures = HarnessJson.Read<JsonElement>(Path.Combine(_root, "Data", "world", "creatures.json"))
            .GetProperty("creatures").EnumerateArray().ToDictionary(x => x.GetProperty("id").GetGuid());
        JsonElement ResolveCreature(Guid id) => creatures.TryGetValue(id, out var value) ? value
            : throw new InvalidDataException($"Unknown creature '{id}'.");
        var input = new IdleBattleInput(scenario.SchemaVersion, scenario, character, area, ResolveCreature(scenario.CreatureId),
            new(seed, MaxTicks: 6000, StartActiveAbilitiesOnCooldown: true,
                BasicAttackIntervalTicks: 30, CaptureEventLog: false, CaptureCompactTelemetry: true),
            threat, cadenceSeconds, scenario.AdditionalCreatureIds?.Select(ResolveCreature).ToArray());
        Validate(input);
        return input;
    }

    public EquipmentReferenceBuild CreateBuild(EquipmentReferenceBuildDefinition build) =>
        new EquipmentReferenceBuildFactory(Equipment, Essences, new SelectedEssenceResolver(Essences))
            .Create(build, requireCompleteLoadout: false);

    public void Validate(IdleBattleInput input)
    {
        ValidateEncounter(input);
        if (input.Character.Id == Guid.Empty || input.Character.Level is < 1 or > 100
            || input.Character.BaseAttributes.Count == 0
            || input.Character.BaseAttributes.Values.Any(x => !float.IsFinite(x) || x < 0))
            throw new InvalidDataException("Invalid character identity, level, or base attributes.");
        if (input.Rules with { RandomSeed = 0, CaptureEventLog = false }
                != new Services.LL.Interfaces.Combat.Resolution.CombatRuleset(0, MaxTicks: 6000, CaptureEventLog: false)
            || HarnessJson.Hash(input.ThreatAndTanking) != HarnessJson.Hash(_threat)
            || !double.IsFinite(input.EncounterCadenceSeconds) || input.EncounterCadenceSeconds <= 0)
            throw new InvalidDataException("This idle slice requires the normal idle rules and a positive cadence.");
        if (input.Character.Essences.Count > EssenceSlotProgression.GetUnlockedSlotCount(input.Character.Level))
            throw new InvalidDataException("The character has not unlocked enough essence slots.");
        var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var essence in input.Character.Essences)
        {
            var definition = Essences.GetById(essence.DefinitionId)
                ?? throw new InvalidDataException($"Unknown essence '{essence.DefinitionId}'.");
            var tier = definition.Ascension.Tiers.SingleOrDefault(x => x.Tier == essence.AscensionTier);
            if (!families.Add(definition.SourceMonsterId) || tier is null
                || essence.Level < tier.MinLevel || essence.Level > tier.MaxLevel)
                throw new InvalidDataException("Invalid essence progression or duplicate monster family.");
        }
        EquipmentReferenceBuildFactory.ValidateEquipmentSlots(
            input.Character.Equipment.Select(x => (x.Slot, x.Data.EquipmentType)).ToArray(), requireCompleteLoadout: false);
        if (input.Character.Equipment.Select(x => x.Data.State.Id).Distinct().Count() != input.Character.Equipment.Count
            || input.Character.Equipment.Any(x => input.Character.Level
                < EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(x.Data.State.Tier)))
            throw new InvalidDataException("Duplicate equipment instances or equipment tier above the character's level.");
    }

    public CombatSetupService CreateSetup(Character character, IReadOnlyList<PlayerEssence> essences) => new(
        new CreatureScaler(_scaling), new SelectedEssenceResolver(Essences, character.Id, essences),
        Essences, _creatureEssences, _creatureAbilities, Equipment);

    public CombatEngineExecutor CreateExecutor() => new(_abilities, Essences, Equipment, Options.Create(_threat));

    public static Area ReadArea(IdleBattleInput input) => input.Area.Deserialize<Area>(HarnessJson.Options)
        ?? throw new InvalidDataException("Missing area.");

    public static Creature ReadCreature(IdleBattleInput input) => input.Creature.Deserialize<Creature>(HarnessJson.Options)
        ?? throw new InvalidDataException("Missing creature.");

    public static IReadOnlyList<Creature> ReadCreatures(IdleBattleInput input) =>
        new[] { input.Creature }.Concat(input.AdditionalCreatures ?? []).Select(snapshot =>
            snapshot.Deserialize<Creature>(HarnessJson.Options) ?? throw new InvalidDataException("Missing creature.")).ToArray();

    // Pure snapshot validation is also used when reading historical comparisons.
    public static void ValidateEncounter(IdleBattleInput input)
    {
        ValidateCreatureSelection(input.Scenario);
        if (input.SchemaVersion != input.Scenario.SchemaVersion
            || (input.SchemaVersion == 1 && input.AdditionalCreatures is not null)
            || (input.AdditionalCreatures?.Count ?? 0) != (input.Scenario.AdditionalCreatureIds?.Count ?? 0))
            throw new InvalidDataException("Unsupported battle input version or mismatched creature snapshots.");
        var area = ReadArea(input);
        var creatures = ReadCreatures(input);
        if (area.Id != input.Scenario.AreaId || input.Character.Level < area.LevelRequirement
            || !creatures.Select(c => c.Id).SequenceEqual(input.Scenario.CreatureIds)
            || creatures.Any(c => !area.Creatures.Any(a => a.CreatureId == c.Id))
            || creatures.Count > area.SpawnProbabilities.Count || area.SpawnProbabilities[creatures.Count - 1] <= 0)
            throw new InvalidDataException("The fixed creature group/character is not eligible for this area.");
    }

    private static void ValidateCreatureSelection(IdleScenario scenario)
    {
        if (scenario.SchemaVersion is not (1 or 2)
            || (scenario.SchemaVersion == 1 && scenario.AdditionalCreatureIds is not null)
            || scenario.AdditionalCreatureIds?.Count is 0 or > 2
            || scenario.CreatureIds.Any(id => id == Guid.Empty))
            throw new InvalidDataException("Version 1 requires one creature; version 2 supports an ordered group of one to three creatures.");
    }

    private sealed class SelectedEssenceResolver(
        IEssenceDefinitionRepository definitions, Guid? characterId = null,
        IReadOnlyList<PlayerEssence>? selected = null) : IEssenceCombatLoadoutResolver
    {
        public EssenceCombatLoadout Resolve(Guid id, IEnumerable<PlayerEssence> essences) =>
            EssenceCombatLoadoutFactory.Create(definitions, id, essences);

        public Task<EssenceCombatLoadout> ResolveAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (id != characterId || selected is null)
                throw new InvalidOperationException($"No offline loadout was supplied for '{id}'.");
            return Task.FromResult(Resolve(id, selected));
        }
    }
}
