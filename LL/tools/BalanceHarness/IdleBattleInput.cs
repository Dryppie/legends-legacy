using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.CombatStyles;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.Combat.Engine;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.PowerRatings;

namespace BalanceHarness;

public sealed record IdleScenario(
    int SchemaVersion, string Id, string CharacterProfile, string AreaId,
    Guid CreatureId, DateTimeOffset StartsAt, IReadOnlyList<string> Assumptions,
    EquipmentReferenceBuildDefinition? Build = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<Guid>? AdditionalCreatureIds = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, int>? EssenceLevels = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    FixtureCombatStyle? CombatStyle = null)
{
    [JsonIgnore]
    public IReadOnlyList<Guid> CreatureIds => [CreatureId, .. AdditionalCreatureIds ?? []];
}

public sealed record FixtureEquipment(EquipmentSlotType Slot, EquipmentData Data);
public sealed record FixtureEssence(string DefinitionId, int Level, int AscensionTier, bool IsEvolved);
public sealed record FixtureCombatStyle(string Id, int Level = 0, string? RefinementId = null,
    // Retired recipe field keeps its historical serialized name for frozen input hashes.
    IReadOnlyList<string>? UpgradeIds = null, string? FocusEssenceDefinitionId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MasteredUpgradeId = null)
{
    [JsonIgnore]
    public string? ChanneledEssenceDefinitionId { get => FocusEssenceDefinitionId; init => FocusEssenceDefinitionId = value; }
}

public sealed record FixtureCharacter(
    Guid Id, string Name, int Level, IReadOnlyDictionary<AttributeType, float> BaseAttributes,
    IReadOnlyList<FixtureEquipment> Equipment, IReadOnlyList<FixtureEssence> Essences,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    CombatStyleSnapshot? CombatStyle = null)
{
    public static FixtureCharacter From(CanonicalEquipmentBuild build) => new(
        build.Character.Id, build.Character.Name, build.Character.Level,
        build.Character.BaseAttributes.ToDictionary(x => x.AttributeType, x => x.Value),
        // This first fixture is the existing single-mace tutorial reference.
        [new(EquipmentSlotType.MainHand, build.Equipment.Single().ProgressionData
            ?? throw new InvalidOperationException("Starter reference requires frozen equipment data."))],
        build.EquippedEssences.Select(x => new FixtureEssence(
            x.EssenceDefinitionId, x.Level, x.AscensionTier, x.IsEvolved)).ToArray());

    public static FixtureCharacter From(EquipmentReferenceBuild build) => new(
        build.Character.Id, build.Character.Name, build.Character.Level,
        build.Character.BaseAttributes.ToDictionary(x => x.AttributeType, x => x.Value),
        build.Character.EquipmentSlots.Where(x => x.EquipmentInstance is not null)
            .OrderBy(x => x.EquipmentSlotType == EquipmentSlotType.OffHand ? 1 : 0)
            .DistinctBy(x => x.EquipmentInstanceId)
            .OrderBy(x => x.EquipmentSlotType)
            .Select(x => new FixtureEquipment(x.EquipmentSlotType, x.EquipmentInstance!.ProgressionData!)).ToArray(),
        build.EquippedEssences.Select(x => new FixtureEssence(
            x.EssenceDefinitionId, x.Level, x.AscensionTier, x.IsEvolved)).ToArray());

    public Character Materialize(EquipmentCatalog catalog)
    {
        var character = new Character
        {
            Id = Id, Name = Name, Level = Level,
            BaseAttributes = BaseAttributes.OrderBy(x => x.Key).Select(x => new EntityAttribute
                { EntityId = Id, AttributeType = x.Key, Value = x.Value }).ToList()
        };
        foreach (var selection in Equipment)
        {
            var item = new EquipmentInstance
            {
                Id = selection.Data.State.Id, ItemBaseId = selection.Data.ItemBaseId,
                ItemBase = catalog.GetEquipmentBase(selection.Data.ItemBaseId)
            };
            item.ApplyProgressionData(selection.Data);
            character.EquipmentSlots.Add(new EquipmentSlot
            {
                EntityId = Id, Entity = character, EquipmentSlotType = selection.Slot,
                EquipmentInstanceId = item.Id, EquipmentInstance = item
            });
        }
        var main = character.EquipmentSlots.SingleOrDefault(x => x.EquipmentSlotType == EquipmentSlotType.MainHand);
        if (main?.EquipmentInstance?.ProgressionData?.EquipmentType == EquipmentType.TwoHanded)
            character.EquipmentSlots.Add(new EquipmentSlot
            {
                EntityId = Id, Entity = character, EquipmentSlotType = EquipmentSlotType.OffHand,
                EquipmentInstanceId = main.EquipmentInstanceId, EquipmentInstance = main.EquipmentInstance
            });
        return character;
    }

    public IReadOnlyList<PlayerEssence> MaterializeEssences() => Essences.Select((x, index) => new PlayerEssence
    {
        Id = Common.Randomness.StableRandom.Guid("balance-essence-v1", Id.ToString("N"),
            index.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        CharacterId = Id, EssenceDefinitionId = x.DefinitionId,
        Level = x.Level, AscensionTier = x.AscensionTier, IsEvolved = x.IsEvolved,
        AbsorbedAt = DateTimeOffset.UnixEpoch, UpdatedAt = DateTimeOffset.UnixEpoch
    }).ToArray();
}

public sealed record IdleBattleInput(
    int SchemaVersion, IdleScenario Scenario, FixtureCharacter Character,
    JsonElement Area, JsonElement Creature, CombatRuleset Rules,
    ThreatAndTankingOptions ThreatAndTanking, double EncounterCadenceSeconds,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<JsonElement>? AdditionalCreatures = null);

public sealed record BattleSummary(
    BattleOutcome EngineOutcome, BattleOutcome ContentOutcome, string TerminationReason,
    int DurationTicks, double DurationSeconds, IReadOnlyList<SimpleCombatEntity> Friendly,
    IReadOnlyList<SimpleCombatEntity> Hostile, IReadOnlyList<EntityStats> Statistics,
    CompactCombatTelemetry Telemetry,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<CombatStyleCombatSummary>? CombatStyles = null)
{
    public static BattleSummary From(CombatResult result, int maxTicks) => new(
        result.EngineOutcome, result.ContentOutcome,
        result.EngineOutcome == BattleOutcome.Draw && result.Duration >= maxTicks
            ? "TickLimit" : result.EngineOutcome.ToString(),
        result.Duration, result.Duration / (double)FastCombatEngine.TicksPerSecond,
        result.PlayerTeam, result.EnemyTeam,
        result.EntityStats.Select(x => x with
        {
            Abilities = x.Abilities.Select(a => a with { Definition = null }).ToList()
        }).ToArray(), result.CompactTelemetry, result.CombatStyles.Count > 0 ? result.CombatStyles : null);
}

public sealed record BattleReport(
    int SchemaVersion, string ScenarioId, int Seed, int TicksPerSecond,
    JsonElement PreparedParticipants, BattleSummary Summary,
    IReadOnlyList<CombatLogItem>? EventLog);
