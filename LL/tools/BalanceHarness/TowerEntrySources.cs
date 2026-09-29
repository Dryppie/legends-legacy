using System.Reflection;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Prophecies;
using Common.Randomness;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Prophecies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.LL.Dungeons;
using Services.LL.Interfaces;
using Services.LL.Inventories;
using Services.LL.JsonDefinitions;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.JsonDefinitions.Reader;
using Services.LL.Levels;
using Services.LL.Prophecies;
using Services.LL.Rewards;
using Services.LL.WorldTower;

namespace BalanceHarness;

public sealed record TowerEntrySourceClaim(string Source, DateTimeOffset At, DateTimeOffset PeriodStart,
    int RequiredKills, ProphecyRewardSnapshot Reward, string ObjectiveType = "KillCreatures", int RequiredProgress = 0);
public sealed record TowerEntrySourceState(Guid Owner, IReadOnlyDictionary<string, int> Items,
    long UnappliedCharacterExperience, long Cinders, long Soulstones, long FateEcho);

/// <summary>
/// Seed-free personal source accounting through production progress, claims and sigil assembly.
/// Offered definitions and their acceptance times are supplied conditions, never generated guarantees.
/// XP is recorded but level growth and all equipment changes are outside this affordability projection.
/// </summary>
public sealed partial class TowerEntrySources
{
    private readonly Dictionary<string, int> items = new();
    private readonly List<PlayerProphecyInstance> instances = [];
    private readonly Dictionary<DateTimeOffset, WeeklyRevelationProgress> weeks = new();
    private readonly Dictionary<DateTimeOffset, DailyProphecyRerollState> rerolls = new();
    private readonly Character character;
    private readonly bool appliesExperience;
    private readonly ProphecyService prophecies;
    private readonly ProphecyRewardResolver resolver;
    private readonly JsonCharacterExperienceProgressionProvider experience;
    private readonly DungeonSigilAssemblyService assembly;
    private readonly IReadOnlyList<ProphecyDefinition> definitions;
    public ProphecyBalanceCatalog Balance { get; }
    public int FragmentCost { get; }
    public List<TowerEntrySourceClaim> Claims { get; } = [];
    public int DuplicateClaimsRejected { get; private set; }

    public TowerEntrySources(string apiRoot, Guid owner, Character? progressingCharacter = null, ILevelingService? leveling = null)
    {
        if (owner == Guid.Empty) throw new InvalidDataException("Personal owner required.");
        if ((progressingCharacter is null) != (leveling is null) || progressingCharacter is not null && progressingCharacter.Id != owner)
            throw new InvalidDataException("Progressing character and native leveling service must be supplied together for this owner.");
        character = progressingCharacter ?? new() { Id = owner, UserId = owner, Level = 30 };
        appliesExperience = progressingCharacter is not null;
        var config = new ConfigurationBuilder().Build();
        var definitionProvider = new JsonProphecyDefinitionProvider(config, apiRoot, HarnessJson.Options);
        var balance = new JsonProphecyBalanceProvider(config, apiRoot, HarnessJson.Options, definitionProvider);
        definitions = definitionProvider.GetAll(); Balance = balance.GetCatalog(); resolver = new(balance);
        experience = new(config, apiRoot, HarnessJson.Options);
        var bases = HarnessJson.Read<JsonElement>(Path.Combine(apiRoot, "Data/items/items.json"))
            .EnumerateArray().ToDictionary(j => j.GetProperty("id").GetString()!, j => j.Deserialize<ItemBase>(HarnessJson.Options)!);
        var itemBases = Boundary<IItemBaseRepository>((m,a) => m.Name switch {
            "GetItemBasesByIdsAsync" => Task.FromResult<IReadOnlyDictionary<string,ItemBase>>(((IReadOnlyCollection<string>)a[0]!).Where(bases.ContainsKey).ToDictionary(id => id, id => bases[id])),
            "AddMissingItemBasesAsync" => AddBases((IEnumerable<ItemBase>)a[0]!),
            _ => throw Unexpected(m)
        });
        Task AddBases(IEnumerable<ItemBase> missing) { foreach (var b in missing) bases.TryAdd(b.Id, b); return Task.CompletedTask; }
        var inventory = Boundary<IInventoryService>((m,a) => {
            if (m.Name != "AddItemsToInventory" || (Guid)a[0]! != owner) throw Unexpected(m);
            foreach (var item in (IEnumerable<InventoryItem>)a[1]!)
            {
                if (item.InventoryId != owner || item.Quantity <= 0) throw new InvalidDataException("Invalid personal grant.");
                var id = item.ItemInstance.ItemBaseId; items[id] = checked(items.GetValueOrDefault(id) + item.Quantity);
            }
            return Task.CompletedTask;
        });
        var inventoryRepository = Boundary<IInventoryRepository>((m,a) => {
            if ((Guid)a[0]! != owner) throw Unexpected(m);
            return m.Name switch {
                "GetInventoryQuantityAsync" => Task.FromResult(items.GetValueOrDefault((string)a[1]!)),
                "GetInventoryQuantitiesAsync" => Task.FromResult<IReadOnlyDictionary<string,int>>(((IReadOnlyCollection<string>)a[1]!).ToDictionary(id => id, id => items.GetValueOrDefault(id))),
                _ => throw Unexpected(m)
            };
        });
        var characters = Boundary<ICharacterService>((m,a) => m.Name == "GetCharacterByCharacterIdAsync" && (Guid)a[0]! == owner
            ? Task.FromResult<Character?>(character) : throw Unexpected(m));
        var repository = Boundary<IProphecyRepository>((m,a) => m.Name switch {
            "SyncDefinitionsAsync" => Task.FromResult(definitions),
            "GetInstancesForPeriodAsync" when (Guid)a[0]! == owner && (Guid)a[1]! == owner => Task.FromResult<IReadOnlyList<PlayerProphecyInstance>>(
                instances.Where(i => i.Scope == (ProphecyScope)a[2]! && i.PeriodStart == (DateTimeOffset)a[3]! && i.PeriodEnd == (DateTimeOffset)a[4]!).ToArray()),
            "AddInstancesAsync" => AddInstances((IReadOnlyCollection<PlayerProphecyInstance>)a[0]!),
            "GetDailyRerollStateAsync" when (Guid)a[0]! == owner && (Guid)a[1]! == owner => Task.FromResult(rerolls.GetValueOrDefault((DateTimeOffset)a[2]!)),
            "AddDailyRerollStateAsync" => AddReroll((DailyProphecyRerollState)a[0]!),
            "GetAcceptedInstancesForProgressWindowAsync" when (Guid)a[0]! == owner => Task.FromResult<IReadOnlyList<PlayerProphecyInstance>>(instances),
            "GetInstanceAsync" when (Guid)a[1]! == owner && (Guid)a[2]! == owner => Task.FromResult(instances.SingleOrDefault(i => i.Id == (Guid)a[0]!)),
            "GetWeeklyProgressAsync" when (Guid)a[0]! == owner && (Guid)a[1]! == owner => Task.FromResult(weeks.GetValueOrDefault((DateTimeOffset)a[2]!)),
            "AddWeeklyProgressAsync" => AddWeek((WeeklyRevelationProgress)a[0]!),
            _ => throw Unexpected(m)
        });
        Task AddInstances(IReadOnlyCollection<PlayerProphecyInstance> added)
        {
            foreach (var i in added)
            {
                if (i.CharacterId != owner || i.PlayerId != owner) throw new InvalidDataException("Cross-owner offer.");
                // Database row GUIDs do not participate in selection. Normalize them for reproducible offline receipts.
                i.Id = StableRandom.Guid("tower-native-offer-row-v1", owner.ToString(), i.Scope.ToString(), i.SlotType.ToString(), i.PeriodStart.ToString("O"));
                if (instances.Any(p => p.Id == i.Id)) throw new InvalidDataException("Duplicate generated offer.");
                instances.Add(i);
            }
            return Task.CompletedTask;
        }
        Task AddReroll(DailyProphecyRerollState state) { rerolls.Add(state.PeriodStart, state); return Task.CompletedTask; }
        Task AddWeek(WeeklyRevelationProgress week) { weeks.Add(week.PeriodStart, week); return Task.CompletedTask; }
        prophecies = new(definitionProvider, balance, resolver, experience, repository, characters,
            Boundary<IEntityService>((m,_) => m.Name == "UpdateEntities" ? null : throw Unexpected(m)),
            // The source ledger records native XP awards, but does not materialize levels or reuse combat outcomes with them.
            leveling ?? Boundary<ILevelingService>((m,_) => m.Name == "UpdateCharacterLevel" ? Task.CompletedTask : throw Unexpected(m)),
            inventory, inventoryRepository, itemBases);
        var tables = new JsonRewardTableDefinitionProvider(config, apiRoot, HarnessJson.Options, new RewardTableDefinitionValidator());
        var dungeons = new JsonDungeonDefinitions(new JsonDocumentReader<DungeonCatalogDocument>(apiRoot, "Data/dungeons/dungeons.json", HarnessJson.Options),
            new(new()), new DungeonDefinitionValidator(), tables);
        var runs = Boundary<IDungeonRunRepository>();
        var access = new DungeonAccessPolicy(runs, inventoryRepository, itemBases, Boundary<IDbContext>(), Options.Create(new WorldTowerOptions()));
        var settings = new JsonDungeonSigilAssemblySettingsProvider(config, apiRoot, HarnessJson.Options);
        FragmentCost = settings.GetSettings().FragmentCost;
        assembly = new(dungeons, access, settings, Boundary<IDungeonSigilAssemblyRepository>((m,a) => {
            if (m.Name != "TrySpendFragmentsAsync" || (Guid)a[0]! != owner) throw Unexpected(m);
            var cost = (long)a[1]!; var stock = items.GetValueOrDefault(SigilFragmentItem.ItemBaseId);
            if (cost <= 0) throw new InvalidDataException("Nonpositive assembly debit.");
            if (cost > stock) return Task.FromResult<long?>(null);
            items[SigilFragmentItem.ItemBaseId] = stock - checked((int)cost);
            return Task.FromResult<long?>(stock - cost);
        }), characters, inventory, inventoryRepository, itemBases, new InventoryItemFactory());
    }

    public TowerEntrySourceState State() => new(character.Id, new SortedDictionary<string,int>(items), appliesExperience ? 0 : character.Experience,
        character.Cinders, character.Soulstones, character.FateEcho);

    public Task<IReadOnlyList<ProphecyProgressUpdate>> TrackEssenceExperience(int amount, DateTimeOffset at, CancellationToken ct) => amount > 0
        ? prophecies.TrackProgressAsync(new ProphecyProgressEvent(character.Id, at, ProphecyProgressKind.EssenceXpGained, amount), ct)
        : throw new ArgumentOutOfRangeException(nameof(amount));

    public Task<PropheciesOverview> Overview(DateTimeOffset at, CancellationToken ct) => prophecies.GetOverviewAsync(character.Id, character.Id, at, ct);
    public Task<IReadOnlyList<ProphecyProgressUpdate>> Track(ProphecyProgressEvent progress, CancellationToken ct) =>
        progress.CharacterId == character.Id ? prophecies.TrackProgressAsync(progress, ct) : throw new InvalidDataException("Cross-owner progress.");
    public async Task<bool> AcceptGeneratedOffer(Guid id, DateTimeOffset at, CancellationToken ct) =>
        (await prophecies.AcceptAsync(character.Id, character.Id, id, at, ct)).Succeeded;
    public Task<IReadOnlyList<ProphecyProgressUpdate>> TrackWins(int count, DateTimeOffset at, CancellationToken ct) => count > 0
        ? prophecies.TrackProgressAsync(new ProphecyProgressEvent(character.Id, at, ProphecyProgressKind.EncounterWon, count, EnemyCount: 1), ct)
        : throw new ArgumentOutOfRangeException(nameof(count));

    public PlayerProphecyInstance AcceptConditionalOffer(string id, DateTimeOffset periodStart, DateTimeOffset acceptedAt)
    {
        var d = definitions.Single(d => d.Id == id);
        if (!d.IsEnabled || d.MinPlayerLevel > character.Level || d.MaxPlayerLevel < character.Level
            || d.RequiredFeatures.Count > 0 || d.RequiredTags.Count > 0 || d.ExcludedTags.Count > 0
            || d.ObjectiveType != ProphecyObjectiveType.KillCreatures || periodStart.Offset != TimeSpan.Zero
            || periodStart.TimeOfDay != TimeSpan.Zero || (d.Scope == ProphecyScope.Weekly && periodStart.DayOfWeek != DayOfWeek.Monday)
            || instances.Any(i => i.Scope == d.Scope && i.PeriodStart == periodStart))
            throw new InvalidDataException("Offer outside the declared eligible one-per-period kill scenario.");
        var end = periodStart.AddDays(d.Scope == ProphecyScope.Daily ? 1 : 7);
        if (acceptedAt < periodStart || acceptedAt >= end) throw new InvalidDataException("Acceptance outside period.");
        var instance = new PlayerProphecyInstance {
            Id = StableRandom.Guid("tower-entry-sources-v1", character.Id.ToString(), id, periodStart.ToString("O")),
            PlayerId = character.Id, CharacterId = character.Id, ProphecyDefinitionId = id, ProphecyDefinition = d,
            Scope = d.Scope, Status = ProphecyStatus.Accepted, PeriodStart = periodStart, PeriodEnd = end,
            AcceptedAt = acceptedAt, GeneratedAt = acceptedAt,
            TargetValue = Balance.Targets.Single(t => t.Scope == d.Scope && t.ObjectiveType == d.ObjectiveType).GetValue(d.Difficulty),
            RewardSnapshotJson = JsonSerializer.Serialize(resolver.Resolve(d, new(30, experience.GetRequiredExperience(30))))
        };
        instances.Add(instance); return instance;
    }
    public Task<IReadOnlyList<ProphecyProgressUpdate>> TrackKills(int count, DateTimeOffset at, CancellationToken ct) => count > 0
        ? prophecies.TrackProgressAsync(new ProphecyProgressEvent(character.Id, at, ProphecyProgressKind.CreatureDefeated, count), ct)
        : throw new ArgumentOutOfRangeException(nameof(count));

    public async Task<bool> Claim(PlayerProphecyInstance instance, DateTimeOffset at, CancellationToken ct)
    {
        var result = await prophecies.ClaimAsync(character.Id, character.Id, instance.Id, at, ct);
        if (!result.Succeeded) return false;
        var objective = instance.ProphecyDefinition!.ObjectiveType;
        Claims.Add(new(instance.ProphecyDefinitionId, at, instance.PeriodStart,
            objective == ProphecyObjectiveType.KillCreatures ? instance.TargetValue : 0, result.Value!.Reward, objective, instance.TargetValue));
        var state = HarnessJson.Hash(State());
        if ((await prophecies.ClaimAsync(character.Id, character.Id, instance.Id, at, ct)).Succeeded || state != HarnessJson.Hash(State()))
            throw new InvalidDataException("Repeated prophecy claim changed stock.");
        DuplicateClaimsRejected++; return true;
    }
    public async Task ClaimMilestones(DateTimeOffset at, CancellationToken ct)
    {
        foreach (var milestone in Balance.WeeklyMilestones.OrderBy(m => m.FavorRequired))
        {
            var result = await prophecies.ClaimWeeklyMilestoneAsync(character.Id, character.Id, milestone.FavorRequired, at, ct);
            if (!result.Succeeded) continue;
            Claims.Add(new("revelation." + milestone.FavorRequired, at, result.Value!.WeeklyRevelation.PeriodStart, 0, result.Value.Reward, "Favor", milestone.FavorRequired));
            var state = HarnessJson.Hash(State());
            if ((await prophecies.ClaimWeeklyMilestoneAsync(character.Id, character.Id, milestone.FavorRequired, at, ct)).Succeeded || state != HarnessJson.Hash(State()))
                throw new InvalidDataException("Repeated milestone claim changed stock.");
            DuplicateClaimsRejected++;
        }
    }
    public async Task<bool> Assemble(string dungeon, int quantity, CancellationToken ct)
    {
        if (dungeon is not ("goblin_mines" or "forgotten_catacombs")) throw new InvalidDataException("Only unlocked region-1 grade-I sources are in scope.");
        return (await assembly.AssembleAsync(character.Id, dungeon, quantity, ct)).Succeeded;
    }
    private static Exception Unexpected(MethodInfo m) => new InvalidOperationException("Unmodeled entry-source boundary: " + m.Name);
    private static T Boundary<T>(Func<MethodInfo, object?[], object?>? call = null) where T : class
    {
        var proxy = DispatchProxy.Create<T, SourceBoundary>();
        ((SourceBoundary)(object)proxy).Call = call ?? ((m,_) => throw Unexpected(m)); return proxy;
    }
    public class SourceBoundary : DispatchProxy
    {
        internal Func<MethodInfo, object?[], object?> Call { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => Call(method!, args ?? []);
    }
}
