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
        if (scenario.SchemaVersion != 1 || string.IsNullOrWhiteSpace(scenario.Id))
            throw new InvalidDataException("Expected a named version-1 idle scenario.");
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
            .GetProperty("creatures").EnumerateArray();
        var creature = creatures.SingleOrDefault(x => x.GetProperty("id").GetGuid() == scenario.CreatureId);
        if (creature.ValueKind == JsonValueKind.Undefined)
            throw new InvalidDataException($"Unknown creature '{scenario.CreatureId}'.");
        var input = new IdleBattleInput(1, scenario, character, area, creature,
            new(seed, MaxTicks: 6000, StartActiveAbilitiesOnCooldown: true,
                BasicAttackIntervalTicks: 30, CaptureEventLog: false, CaptureCompactTelemetry: true),
            threat, cadenceSeconds);
        Validate(input);
        return input;
    }

    public EquipmentReferenceBuild CreateBuild(EquipmentReferenceBuildDefinition build) =>
        new EquipmentReferenceBuildFactory(Equipment, Essences, new SelectedEssenceResolver(Essences))
            .Create(build, requireCompleteLoadout: false);

    public void Validate(IdleBattleInput input)
    {
        if (input.SchemaVersion != 1 || input.Scenario.SchemaVersion != 1)
            throw new InvalidDataException("Unsupported battle input version.");
        if (input.Character.Id == Guid.Empty || input.Character.Level is < 1 or > 100
            || input.Character.BaseAttributes.Count == 0
            || input.Character.BaseAttributes.Values.Any(x => !float.IsFinite(x) || x < 0))
            throw new InvalidDataException("Invalid character identity, level, or base attributes.");
        var area = ReadArea(input);
        var creature = ReadCreature(input);
        if (area.Id != input.Scenario.AreaId || creature.Id != input.Scenario.CreatureId
            || !area.Creatures.Any(x => x.CreatureId == creature.Id)
            || input.Character.Level < area.LevelRequirement)
            throw new InvalidDataException("The fixed creature/character is not eligible for this area.");
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
