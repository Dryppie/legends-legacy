using Application.Interfaces.Services.LL.Items;
using System.Text.Json;
using Application.UseCases.Inventories.SelectionCrates;
using Common.Randomness;
using Domain.Models.Combat;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Items;
using Services.LL.PowerRatings;

namespace BalanceHarness;

public sealed record TowerActivityRewards(int From, int Until, string Area, int Victories,
    IReadOnlyList<EquipmentData> Equipment, IReadOnlyDictionary<string, int> Sigils);

/// <summary>Production idle reward processor with file-backed item metadata and retained personal awards.</summary>
public sealed class TowerActivityInventory
{
    public const string Version = "tower-joint-activity-v1";
    public static readonly DateTimeOffset Epoch = DateTimeOffset.UnixEpoch;
    private readonly OfflineContent content;
    private readonly CombatAcquisitionRewardProcessor processor;
    public TowerActivityInventory(string root, OfflineContent content)
    {
        this.content = content;
        Catalog = JsonStarterEquipmentCatalog.LoadOrdinary(content.Equipment, Path.Combine(root, "Data/equipment/equipment-ordinary.v1.json"));
        var blueprints = JsonEquipmentBlueprintCatalog.Load(Path.Combine(root, "Data/equipment/equipment-blueprints.v1.json"), content.Equipment);
        var items = HarnessJson.Read<System.Text.Json.JsonElement>(Path.Combine(root, "Data/items/items.json"));
        // Item metadata must exist in the actual seed catalog; persistence is deliberately absent.
        var source = items.ValueKind == System.Text.Json.JsonValueKind.Array ? items.EnumerateArray().ToArray()
            : items.GetProperty("items").EnumerateArray().ToArray();
        var bases = new Dictionary<string, ItemBase>();
        foreach (var archetype in content.Equipment.Evaluator.Definitions.Select(d => content.Equipment.Evaluator.GetArchetype(d.ArchetypeId)).DistinctBy(a => a.Id))
        {
            var data = source.Single(i => i.GetProperty("id").GetString() == archetype.ItemBaseId);
            var item = data.Deserialize<EquipmentBase>(HarnessJson.Options)!;
            if (item.EquipmentType != archetype.EquipmentType) throw new InvalidDataException("Equipment item base type mismatch.");
            bases[archetype.ItemBaseId] = item;
        }
        foreach (var sigil in Catalog.Pools.SelectMany(p => p.Sigils))
        {
            var data = source.Single(i => i.GetProperty("id").GetString() == sigil.ItemBaseId);
            bases[sigil.ItemBaseId] = data.Deserialize<ItemBase>(HarnessJson.Options)!;
        }
        processor = new(Catalog, new ItemBases(bases), Options.Create(new EquipmentProgressionOptions()), new Entitlements(), blueprints);
    }
    public CombatAcquisitionCatalog Catalog { get; }
    public static bool Victory(string scenario, int ordinal) => scenario switch
    {
        "perfect" => true, "four-of-five" => ordinal % 5 != 0, _ => throw new InvalidDataException("Unknown idle outcome scenario.")
    };
    public async Task<TowerActivityRewards> Rewards(Guid owner, string scenario, string area, int from, int until, int cadence, CancellationToken token)
    {
        if (from < 0 || until <= from || until > 86400 || cadence <= 0) throw new InvalidDataException("Invalid bounded activity window.");
        var encounters = Enumerable.Range(from, until - from).Select(i => new IdleEncounterRewardFacts(
            StableRandom.Guid(Version, owner.ToString(), i.ToString()), i + 1, Epoch.AddSeconds((long)i * cadence),
            Victory(scenario, i + 1) ? BattleOutcome.Victory : BattleOutcome.Defeat, [], [], null!)).ToArray();
        var facts = new IdleCombatRewardFacts(owner, Epoch.AddSeconds((long)from * cadence), Epoch.AddSeconds((long)until * cadence),
            Epoch.AddSeconds((long)until * cadence), TimeSpan.FromSeconds((long)(until - from) * cadence),
            new Area { Id = area, Name = area }, [owner], encounters) { ScheduleGeneration = 1 };
        var awards = await processor.ProcessAsync(facts, token);
        return new(from, until, area, encounters.Count(e => e.IsVictory),
            awards.Equipment.Select(i => ((EquipmentInstance)i.ItemInstance).ProgressionData!).ToArray(),
            awards.Sigils.ToDictionary(i => i.ItemInstance.ItemBaseId, i => i.Quantity));
    }
    public EquipmentData QuestMace(Guid owner) => Award(owner, Catalog.BaseDropDefinitions(EquipmentRarity.Common)
        .Single(d => d.ArchetypeId == "plain.mace" && d.SpecializationId == "default").Id, "item.arms_chest", "mace");
    public EquipmentData QuestArmor(Guid owner, int seed)
    {
        var box = RandomEquipmentBoxCatalog.ArmorChest.RandomEquipment!;
        var pool = Catalog.BaseDropDefinitions(box.Rarity).Where(d => box.EquipmentTypes!.Contains(content.Equipment.Evaluator.GetArchetype(d.ArchetypeId).EquipmentType)).ToArray();
        if (box.Quantity != 1 || box.Tier != 1 || box.Rank != 0) throw new InvalidDataException("Changed quest armor budget.");
        // The live box uses a fresh Guid opening identity. This diagnostic freezes its random seed, not its outcome.
        return Award(owner, pool[new Random(seed).Next(pool.Length)].Id, RandomEquipmentBoxCatalog.ArmorChestItemBaseId, "armor");
    }
    private EquipmentData Award(Guid owner, string definition, string source, string key) => EquipmentData.Create(
        EquipmentState.Award(StableRandom.Guid(Version, owner.ToString(), key), content.Equipment.Evaluator, definition, 1, 0,
            new(EquipmentAwardKind.ProtectedReward, source, key), new(EquipmentOwnershipKind.UnboundPersonal, owner)), content.Equipment.Evaluator);

    public double Score(EquipmentData item) => item.Stats.Sum(s => s.Value * content.Equipment.Evaluator.Balance.GetMaterializedCostPerPoint(s.Key, item.State.Tier));
    public IReadOnlyList<FixtureEquipment> Select(IReadOnlyList<EquipmentData> owned)
    {
        // A deterministic inventory policy, not a build search or a claim about combat strength.
        EquipmentData? Best(EquipmentType type) => owned.Where(i => i.EquipmentType == type)
            .OrderByDescending(Score).ThenBy(i => i.State.Id.ToString(), StringComparer.Ordinal).FirstOrDefault();
        var equipped = new List<FixtureEquipment>();
        foreach (var type in new[] { EquipmentType.Head, EquipmentType.Chest, EquipmentType.Legs, EquipmentType.Necklace, EquipmentType.Ring, EquipmentType.Relic })
            if (Best(type) is { } item) equipped.Add(new(Enum.Parse<EquipmentSlotType>(type.ToString()), item));
        var one = Best(EquipmentType.OneHanded); var off = Best(EquipmentType.OffHand); var two = Best(EquipmentType.TwoHanded);
        if (two is not null && (one is null || Score(two) > Score(one) + (off is null ? 0 : Score(off))))
            equipped.Add(new(EquipmentSlotType.MainHand, two));
        else if (one is not null)
        {
            equipped.Add(new(EquipmentSlotType.MainHand, one));
            if (off is not null) equipped.Add(new(EquipmentSlotType.OffHand, off));
        }
        EquipmentReferenceBuildFactory.ValidateEquipmentSlots(equipped.Select(e => (e.Slot, e.Data.EquipmentType)).ToArray(), requireCompleteLoadout: false);
        return equipped.OrderBy(e => e.Slot).ToArray();
    }
    private sealed class ItemBases(IReadOnlyDictionary<string, ItemBase> items) : IItemBaseRepository
    {
        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(ids.ToDictionary(id => id, id => items[id]));
        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> items, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class Entitlements : IPlainEquipmentRepository
    {
        public Task<IReadOnlyList<PlainEquipmentEntitlement>> GetAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task RecordAwardAsync(Guid owner, EquipmentData item, CancellationToken ct)
        {
            if (item.State.Ownership.OwnerId != owner) throw new InvalidDataException("Cross-owner reward.");
            return Task.CompletedTask;
        }
    }
}
