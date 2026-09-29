using System.Reflection;
using System.Text.Json;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Items;
using Common.Randomness;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Rewards.Dungeon;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Inventories;
using Services.LL.Items;
using Services.LL.JsonDefinitions;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.JsonDefinitions.Reader;
using Services.LL.Rewards;

namespace BalanceHarness;

public sealed record TowerBlueprintState(string Family, int Misses, Guid? LastRunId);
public sealed record TowerDungeonLootRoll(string Source, int? Room, IReadOnlyList<RunReward> Awards);
public sealed record TowerDungeonLootResult(Guid RunId, IReadOnlyList<TowerBlueprintState> Before,
    IReadOnlyList<TowerBlueprintState> After, IReadOnlyList<TowerDungeonLootRoll> Rolls,
    IReadOnlyList<RunReward> Lost, IReadOnlyList<RunReward> Pending,
    IReadOnlyList<EquipmentData> Equipment, IReadOnlyDictionary<string, int> Blueprints, int RetryChecks);

/// <summary>
/// Applies production equipment/blueprint acquisition and claiming to a recorded run's eligible boundaries.
/// Entry equipment remains fixed during the run. The caller feeds claimed items into subsequent attempts only.
/// Currency, cores, creature loot, crafting and mastery growth are deliberately outside this diagnostic.
/// </summary>
public sealed class TowerDungeonLoot
{
    public const string Version = "tower-dungeon-loot-v1";
    public static TowerSourcePolicyPlan Read(string fixtures)
    {
        var p = TowerContractJson.Read<TowerSourcePolicyPlan>(Path.Combine(fixtures, "tower-dungeon-loot.json"));
        if (p.Version != Version || p.ActivityVersion != TowerActivityInventory.Version
            || !p.Policies.SequenceEqual(new[] { "supplies-only", "include-dungeon-loot" })
            || p.PanelIndex != "family-attempt-ordinal" || string.IsNullOrWhiteSpace(p.Assumptions))
            throw new InvalidDataException("Changed loot policy requires a new declaration.");
        return p;
    }
    private readonly string root;
    private readonly OfflineContent content;
    private readonly JsonDungeonDefinitions definitions;
    private readonly EquipmentBlueprintCatalog blueprints;
    private readonly Dictionary<string, EquipmentBlueprintProgress> progress = new();
    private readonly Dictionary<string, ItemBase> itemBases;

    public TowerDungeonLoot(string root, OfflineContent content)
    {
        this.root = root; this.content = content;
        var config = new ConfigurationBuilder().Build();
        var tables = new JsonRewardTableDefinitionProvider(config, root, HarnessJson.Options, new RewardTableDefinitionValidator());
        definitions = new(new JsonDocumentReader<DungeonCatalogDocument>(root, "Data/dungeons/dungeons.json", HarnessJson.Options),
            new(new()), new DungeonDefinitionValidator(), tables);
        blueprints = JsonEquipmentBlueprintCatalog.Load(Path.Combine(root, "Data/equipment/equipment-blueprints.v1.json"), content.Equipment);
        itemBases = HarnessJson.Read<JsonElement>(Path.Combine(root, "Data/items/items.json")).EnumerateArray().ToDictionary(
            j => j.GetProperty("id").GetString()!, j => j.TryGetProperty("equipmentType", out _)
                ? (ItemBase)j.Deserialize<EquipmentBase>(HarnessJson.Options)! : j.Deserialize<ItemBase>(HarnessJson.Options)!);
    }
    public TowerDungeonLoot Fork()
    {
        var copy = new TowerDungeonLoot(root, content);
        foreach (var (key, p) in progress) copy.progress.Add(key, new() { CharacterId = p.CharacterId, FamilyId = p.FamilyId, Misses = p.Misses, LastRunId = p.LastRunId });
        return copy;
    }
    public static TowerDungeonLoot Restore(string root,OfflineContent content,Guid owner,IReadOnlyList<TowerBlueprintState> saved)
    {
        if(owner==Guid.Empty||saved.Select(p=>p.Family).Distinct().Count()!=saved.Count||saved.Any(p=>p.Misses<0))
            throw new InvalidDataException("Invalid personal blueprint checkpoint.");
        var result=new TowerDungeonLoot(root,content);
        foreach(var p in saved)result.progress.Add(p.Family,new() {CharacterId=owner,FamilyId=p.Family,Misses=p.Misses,LastRunId=p.LastRunId});
        return result;
    }
    private IReadOnlyList<TowerBlueprintState> State() => progress.Values.OrderBy(p => p.FamilyId)
        .Select(p => new TowerBlueprintState(p.FamilyId, p.Misses, p.LastRunId)).ToArray();

    public async Task<TowerDungeonLootResult> ApplyAsync(Guid owner, DungeonAcquisitionRun recorded, CancellationToken ct,
        int masteryLevel = 0, bool resolvedRouteRewards = false)
    {
        if (owner == Guid.Empty || recorded.Status is not (DungeonRunStatus.Completed or DungeonRunStatus.Failed)
            || recorded.Actions.Any(a => a.Type == RoomType.Treasury)) throw new InvalidDataException("Only the declared terminal, non-treasury route is supported.");
        var dungeon = definitions.GetByKey(recorded.Dungeon);
        if (dungeon.Region != 1 || dungeon.Tier != 1) throw new InvalidDataException("Changed reward scope.");
        var before = State();
        if (masteryLevel is < 0 or > 10) throw new InvalidDataException("Invalid loot entry mastery.");
        var run = new DungeonRun {
            Id = StableRandom.Guid("dungeon-acquisition-run-v1", owner.ToString(), recorded.Dungeon, recorded.LayoutSeed.ToString()),
            CharacterId = owner, DungeonDefinitionId = recorded.Dungeon, Seed = recorded.LayoutSeed,
            Rooms = recorded.Layout.GetProperty("rooms").Deserialize<List<RoomInstance>>(HarnessJson.Options)!,
            CreatedAt = DateTimeOffset.UnixEpoch, CompletedAt = DateTimeOffset.UnixEpoch
        };
        run.State.MasteryLevelAtStart = masteryLevel;
        var repository = Boundary<IDungeonRunRepository>((m,a) => {
            if (m.Name != "AddPendingRewardAsync" || !ReferenceEquals(a[0], run)) throw Unexpected(m);
            var reward = (RunReward)a[1]!;
            if (run.PendingRewards.Any(r => r.Id == reward.Id)) throw new InvalidDataException("Duplicate pending loot.");
            run.PendingRewards.Add(reward); return Task.FromResult(true);
        });
        var blueprintRepository = Boundary<IEquipmentBlueprintRepository>((m,a) => {
            if (m.Name != "LoadForCompletionAsync" || (Guid)a[0]! != owner) throw Unexpected(m);
            var family = (string)a[1]!;
            if (!progress.TryGetValue(family, out var p)) progress[family] = p = new() { CharacterId = owner, FamilyId = family };
            if (p.CharacterId != owner) throw new InvalidDataException("Cross-owner blueprint state.");
            return Task.FromResult(p);
        });
        var settings = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(Path.Combine(root, "appsettings.json"))).Build()
            .GetSection("EquipmentProgression").Get<EquipmentProgressionOptions>() ?? new();
        var acquisition = new EquipmentAcquisitionService(JsonStarterEquipmentCatalog.LoadOrdinary(content.Equipment,
            Path.Combine(root, "Data/equipment/equipment-ordinary.v1.json")), definitions, repository, Options.Create(settings), blueprints, blueprintRepository);
        var rolls = new List<TowerDungeonLootRoll>(); var retryChecks = 0;
        async Task Roll(string source, int? room)
        {
            var count = run.PendingRewards.Count;
            Task Invoke() => room.HasValue ? acquisition.CompleteMiniBossAsync(run, room.Value, ct) : acquisition.CompleteAsync(run, false, ct);
            await Invoke();
            var expected = HarnessJson.Hash(new { run.PendingRewards, progress = State() });
            await Invoke();
            if (expected != HarnessJson.Hash(new { run.PendingRewards, progress = State() })) throw new InvalidDataException("Reward retry rerolled or duplicated loot.");
            retryChecks++;
            rolls.Add(new(source, room, run.PendingRewards.Skip(count).ToArray()));
        }
        foreach (var action in recorded.Actions)
        {
            if ((action.Action == "fight" || resolvedRouteRewards && action.Action == "choose_route")
                && action.Type == RoomType.MiniBoss && action.Status == DungeonRunStatus.Active)
            {
                run.Rooms.Single(r => r.RoomIndex == action.Room).Status = RoomInstanceStatus.Completed;
                await Roll("dungeon-miniboss", action.Room);
            }
        }
        var lost = recorded.Status == DungeonRunStatus.Failed ? run.PendingRewards.ToArray() : [];
        // Production DungeonRunService.FailRun clears pending loot; no claim is allowed on failure.
        if (recorded.Status == DungeonRunStatus.Failed) run.PendingRewards.Clear();
        run.Status = recorded.Status;
        if (run.Status == DungeonRunStatus.Completed) await Roll(EquipmentKeys.DungeonCompletionSource, null);
        var pending = run.PendingRewards.ToArray();
        var claimed = new List<InventoryItem>();
        if (run.Status == DungeonRunStatus.Completed)
        {
            var bases = Boundary<IItemBaseRepository>((m,a) => m.Name == "GetItemBasesByIdsAsync"
                ? Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(((IReadOnlyCollection<string>)a[0]!).ToDictionary(id => id, id => itemBases[id])) : throw Unexpected(m));
            var inventory = Boundary<IInventoryService>((m,a) => {
                if (m.Name != "AddItemsToInventory" || (Guid)a[0]! != owner) throw Unexpected(m);
                claimed.AddRange((List<InventoryItem>)a[1]!); return Task.CompletedTask;
            });
            var claimer = new DungeonRunRewardClaimer(Boundary<IExperienceRewardWriter>(), Boundary<ICurrencyRewardWriter>(),
                bases, new InventoryItemFactory(), inventory);
            var items = await claimer.ClaimAsync(run, ct);
            if (items.Count != claimed.Count) throw new InvalidDataException("Claim inventory mismatch.");
            // The production run service sets these after a successful claim.
            run.Status = DungeonRunStatus.RewardsClaimed; run.RewardsClaimedAt = DateTimeOffset.UnixEpoch;
            if ((await claimer.ClaimAsync(run, ct)).Count != 0) throw new InvalidDataException("Duplicate claim.");
            retryChecks++;
        }
        return new(run.Id, before, State(), rolls, lost, pending,
            claimed.Where(i => i.ItemInstance is EquipmentInstance).Select(i => ((EquipmentInstance)i.ItemInstance).ProgressionData!).ToArray(),
            claimed.Where(i => i.ItemInstance is not EquipmentInstance).GroupBy(i => i.ItemInstance.ItemBaseId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity)), retryChecks);
    }

    private static Exception Unexpected(MethodInfo m) => new InvalidOperationException("Unmodeled loot boundary: " + m.Name);
    private static T Boundary<T>(Func<MethodInfo, object?[], object?>? call = null) where T : class
    {
        var proxy = DispatchProxy.Create<T, FileBoundary>();
        ((FileBoundary)(object)proxy).Call = call ?? ((m,_) => throw Unexpected(m)); return proxy;
    }
    public class FileBoundary : DispatchProxy
    {
        internal Func<MethodInfo, object?[], object?> Call { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Call(targetMethod!, args ?? []);
    }
}
