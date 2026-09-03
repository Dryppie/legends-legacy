using Services.LL.WorldTower;
using System.Text.Json;
using Application.Common.Mappings;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Regions;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Equipments.Commands.SelectCombatAcquisition;
using Application.MediatR.Behaviors;
using Common.Primitives;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using AutoMapper;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Combat;
using Domain.Models.Bonuses;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Professions.Crafting;
using Domain.Models.Quests;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Services.LL.Quests;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Persistence.LL.Repositories.Quests;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Idle;
using Services.LL.Inventories;
using Services.LL.Items;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.CharacterActions;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Outbox;

namespace EssenceSystem.Tests;

public sealed partial class CombatAcquisitionTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTimeOffset Epoch = new(2026, 9, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Runtime_rules_match_all_authored_region_one_sources_without_changing_starters()
    {
        var equipment = Equipment();
        var catalog = Catalog(equipment);
        Assert.Equal(31, equipment.Options.Count);
        Assert.Equal(10, catalog.Pools[0].Areas.Count);
        var protection = JsonStarterEquipmentCatalog.LoadAcquisition(equipment, Path.Combine(Content(), "equipment-protection-pools.v1.json"));
        foreach (var sigil in catalog.Pools[0].Sigils)
        {
            var pool = protection.Pools.Single(x => x.FamilyId == sigil.FamilyId && x.Difficulty == 1);
            Assert.Equal(pool.MinimumLevel, sigil.MinimumLevel);
            Assert.Equal(pool.RequiredQuestId, sigil.RequiredQuestId);
        }
        Assert.Equal(360, catalog.Pools[0].PlainTargetVictories);
        Assert.Equal(4320, catalog.Pools[0].SigilVictories);
        Assert.Equal(0.0003, catalog.Pools[0].DiscoveryChance);
        Assert.Throws<ArgumentException>(() => new CombatAcquisitionCatalog(equipment, [catalog.Pools[0] with { DiscoveryChance = double.NaN }]));
        Assert.Throws<ArgumentException>(() => new CombatAcquisitionCatalog(equipment, [catalog.Pools[0] with { VictoriesPerPerfectDay = 100 }]));
        Assert.True(new EquipmentProgressionOptions().OrdinaryAcquisitionEnabled);
    }

    [Fact]
    public async Task Full_day_and_small_batches_produce_identical_discoveries_progress_and_sigils()
    {
        var id = Guid.NewGuid();
        await using var full = await Fixture.Create(id, chance: 0.01);
        await using var split = await Fixture.Create(id, chance: 0.01);
        foreach (var f in new[] { full, split })
        {
            await f.Process(-1, 1, win: false);
            await f.Select("plain.staff", "goblin_mines");
        }
        var one = await full.Process(0, 8640);
        var many = new List<InventoryItem>();
        for (var cursor = 0; cursor < 8640; cursor += 37)
            many.AddRange(Flatten(await split.Process(cursor, Math.Min(37, 8640 - cursor))));
        Assert.Equal(2, one.Sigils.Sum(x => x.Quantity));
        Assert.Equal(2, many.Where(x => x.ItemInstance.ItemBaseId == "sigil_goblin_mines").Sum(x => x.Quantity));
        var fullDiscoveries = one.Equipment.Select(x => ((EquipmentInstance)x.ItemInstance).ProgressionData!)
            .Where(x => x.State.Provenance.Kind == EquipmentAwardKind.RandomDiscovery).Select(x => JsonSerializer.Serialize(x)).ToArray();
        var splitDiscoveries = many.Select(x => (x.ItemInstance as EquipmentInstance)?.ProgressionData)
            .Where(x => x?.State.Provenance.Kind == EquipmentAwardKind.RandomDiscovery).Select(x => JsonSerializer.Serialize(x));
        Assert.NotEmpty(fullDiscoveries);
        Assert.Equal(fullDiscoveries, splitDiscoveries);
        Assert.Single(one.Equipment, x => ((EquipmentInstance)x.ItemInstance).ProgressionData!.State.Provenance.Kind == EquipmentAwardKind.ProtectedReward);
        var fullState = await full.Service.GetAsync(id, Ct);
        var splitState = await split.Service.GetAsync(id, Ct);
        Assert.Equal(JsonSerializer.Serialize(fullState), JsonSerializer.Serialize(splitState));
        Assert.Null(fullState[0].SelectedDefinitionId);
    }

    [Fact]
    public async Task Exact_target_survives_switches_pauses_after_award_and_retries_never_rearm_it()
    {
        await using var f = await Fixture.Create();
        await f.Process(0, 1, win: false);
        await f.Select("plain.staff");
        await f.Process(1, 200);
        await f.Select(null);
        await f.Process(201, 20);
        Assert.Equal(200, (await f.Progress()).PlainVictories);
        var operation = Guid.NewGuid();
        await f.Select("plain.dagger", operation: operation);
        var frozen = (await f.Progress()).Plain!.Equipment;
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var result = await f.Process(221, 160);
        var target = Assert.Single(result.Equipment).ItemInstance as EquipmentInstance;
        Assert.Equal(JsonSerializer.Serialize(frozen), JsonSerializer.Serialize(target!.ProgressionData));
        Assert.True(target.IsBound);
        Assert.Equal(0, target.ProgressionData!.State.Rank);
        Assert.Equal(1, Assert.Single(await new PlainEquipmentRepository(f.Db).GetAsync(f.Id, Ct)).Copies);
        await f.Select("plain.dagger", operation: operation);
        Assert.Null((await f.Progress()).Plain);
        Assert.Empty((await f.Process(381, 400)).Equipment);
        Assert.NotNull((await f.Service.SelectAsync(f.Id, operation, f.Catalog.Pools[0].PoolId, "plain.staff", null, Ct)).Error);
        await f.Select("plain.dagger");
        Assert.NotNull((await f.Progress()).Plain);
        Assert.Equal(0, (await f.Progress()).PlainVictories);
    }

    [Fact]
    public async Task Discovery_preserves_authored_state_and_does_not_cancel_target()
    {
        await using var f = await Fixture.Create(chance: 1);
        await f.Process(0, 1, win: false);
        await f.Select("plain.staff");
        var result = await f.Process(1, 3);
        Assert.Equal(3, result.Equipment.Count);
        Assert.All(result.Equipment, x =>
        {
            var data = ((EquipmentInstance)x.ItemInstance).ProgressionData!;
            Assert.False(x.ItemInstance.IsBound);
            Assert.Equal(0, data.State.Rank);
            Assert.Null(data.State.ActiveStyleId);
            Assert.Equal(f.Id, data.State.Ownership.OwnerId);
        });
        Assert.Equal(3, result.Equipment.Select(x => x.ItemInstanceId).Distinct().Count());
        Assert.Equal(3, (await f.Progress()).PlainVictories);
        Assert.Equal("plain.staff", (await f.Progress()).Plain!.Equipment.State.DefinitionId);
    }

    [Fact]
    public async Task Replay_and_overlap_cannot_pay_twice_even_after_inventory_settlement_and_context_reload()
    {
        await using var f = await Fixture.Create(chance: 1);
        var first = await f.Process(0, 2);
        await f.Settle(first);
        f.Db.ChangeTracker.Clear();
        var retry = await f.Process(0, 2);
        Assert.Empty(Flatten(retry));
        var overlap = await f.Process(1, 2);
        await f.Settle(overlap);
        Assert.Single(overlap.Equipment);
        Assert.Equal(3, await f.Db.InventoryItems.CountAsync());
        Assert.Equal(3, await f.Db.EconomyLedger.CountAsync());
        var obsoleteGeneration = f.Facts(100, 1) with { ScheduleGeneration = 0 };
        Assert.Empty(Flatten(await f.Processor().ProcessAsync(obsoleteGeneration, Ct)));
        Assert.Equal(Epoch.AddSeconds(20), (await f.Progress()).LastEncounterAtUtc);
    }

    [Fact]
    public async Task Sigil_selection_requires_unlock_and_switching_keeps_one_shared_access_counter()
    {
        await using var f = await Fixture.Create(eligible: false);
        await f.Process(0, 1, win: false);
        Assert.NotNull((await f.Service.SelectAsync(f.Id, Guid.NewGuid(), f.Catalog.Pools[0].PoolId, null, "goblin_mines", Ct)).Error);
        f.Character.Level = 20;
        await f.Db.SaveChangesAsync();
        Assert.NotNull((await f.Service.SelectAsync(f.Id, Guid.NewGuid(), f.Catalog.Pools[0].PoolId, null, "goblin_mines", Ct)).Error);
        await f.Unlock();
        await f.Select(null, "goblin_mines");
        await f.Process(1, 4319);
        await f.Select(null, "forgotten_catacombs");
        var result = await f.Process(4320, 1);
        Assert.Equal("sigil_forgotten_catacombs", Assert.Single(result.Sigils).ItemInstance.ItemBaseId);
        Assert.True(result.Sigils[0].ItemInstance.IsBound);
        Assert.Equal(0, (await f.Progress()).SigilVictories);
        await f.Select(null);
        Assert.Empty((await f.Process(4321, 5000)).Sigils);
    }

    [Theory]
    [InlineData("training", true)]
    [InlineData("region_03_area_01", true)]
    [InlineData("region_01_area_01", false)]
    public async Task Training_other_regions_and_disabled_feature_do_not_create_progress_or_rewards(string area, bool enabled)
    {
        await using var f = await Fixture.Create(chance: 1);
        f.Flags.OrdinaryAcquisitionEnabled = enabled;
        Assert.Empty(Flatten(await f.Process(0, 500, area: area)));
        Assert.Empty(f.Db.CombatAcquisitionProgress.Local);
        Assert.False(f.Db.ChangeTracker.HasChanges());
        if (!enabled) Assert.Empty(await f.Service.GetAsync(f.Id, Ct));
    }

    [Fact]
    public async Task Selection_settles_old_target_before_changing_and_backlog_does_not_record_new_request()
    {
        await using var f = await Fixture.Create();
        await f.Process(0, 1, win: false);
        await f.Select("plain.staff");
        await f.Process(1, 359);
        f.Actions.Action = new CharacterAction(f.Id, new CombatActionDetails([f.Id], new Area()), Epoch);
        f.Actions.Resolve = async () =>
        {
            var oldReward = await f.Process(360, 1);
            Assert.Equal("plain.staff", ((EquipmentInstance)Assert.Single(oldReward.Equipment).ItemInstance).ProgressionData!.State.DefinitionId);
            f.Actions.Action.ProcessedCount = 1;
            f.Actions.Action.HasMoreDueWork = true;
        };
        var operation = Guid.NewGuid();
        var blocked = await f.Service.SelectAsync(f.Id, operation, f.Catalog.Pools[0].PoolId, "plain.dagger", null, Ct);
        Assert.NotNull(blocked.Error);
        Assert.Null((await f.Progress()).Plain);
        Assert.DoesNotContain(f.Db.CombatAcquisitionSelectionReceipts.Local, x => x.OperationId == operation);
        Assert.True(f.Sync.Invalidations > 0);
        f.Actions.Resolve = () => { f.Actions.Action.HasMoreDueWork = false; return Task.CompletedTask; };
        await f.Select("plain.dagger", operation: operation);
        Assert.Equal("plain.dagger", (await f.Progress()).Plain!.Equipment.State.DefinitionId);
        Assert.Equal(0, (await f.Progress()).PlainVictories);
        var calls = f.Actions.ResolveCalls;
        await f.Select("plain.dagger", operation: operation);
        Assert.Equal(calls, f.Actions.ResolveCalls);
    }

    [Fact]
    public async Task Frozen_target_and_threshold_survive_content_reload()
    {
        await using var f = await Fixture.Create();
        await f.Process(0, 1, win: false);
        await f.Select("plain.staff", "goblin_mines");
        var frozen = (await f.Progress()).Plain!.Equipment;
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var changed = new CombatAcquisitionCatalog(f.Catalog.Equipment, [f.Catalog.Pools[0] with { PlainTargetVictories = 1, SigilVictories = 1 }]);
        Assert.Empty((await f.Processor(changed).ProcessAsync(f.Facts(1, 359), Ct)).Equipment);
        var result = await f.Processor(changed).ProcessAsync(f.Facts(360, 1), Ct);
        Assert.Equal(frozen.State.Id, Assert.Single(result.Equipment).ItemInstanceId);
        Assert.Empty(result.Sigils);
        Assert.Equal(360, (await f.Progress()).SigilVictories);
    }

    [Fact]
    public async Task Invalid_reward_definitions_abort_instead_of_losing_earned_rewards()
    {
        await using var f = await Fixture.Create();
        await f.Process(0, 1, win: false);
        await f.Select("plain.staff");
        f.Db.ItemBases.Remove(await f.Db.ItemBases.SingleAsync(x => x.Id == "staff"));
        await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Process(1, 360));
        Assert.Empty(await f.Db.InventoryItems.ToListAsync());
    }

    [Fact]
    public async Task Concurrent_commands_settling_the_same_boundary_award_only_one_target()
    {
        await using var f = await Fixture.Create();
        await f.Process(0, 1, win: false);
        await f.Select("plain.staff");
        await f.Process(1, 359);
        await f.Db.SaveChangesAsync();
        async Task<Response<CombatAcquisitionDto>> Change()
        {
            await using var db = new LLDbContext(f.DbOptions);
            var actions = new Actions { Action = new CharacterAction(f.Id, new CombatActionDetails([f.Id], new Area()), Epoch) };
            actions.Resolve = async () =>
            {
                var awards = await f.Processor(db: db).ProcessAsync(f.Facts(360, 1), Ct);
                await new InventoryService(new InventoryRepository(db)).AddItemsToInventory(f.Id, Flatten(awards).ToList(), "combat-reward", Ct);
                actions.Action.ProcessedCount = 1;
            };
            var sync = new Sync();
            var service = new CombatAcquisitionService(f.Catalog, new CombatAcquisitionRepository(db), new QuestRepository(db), actions, sync, Options.Create(f.Flags), Options.Create(new WorldTowerOptions()));
            var mapper = new MapperConfiguration(x => x.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
            var handler = new SelectCombatAcquisitionCommandHandler(service, mapper);
            var request = new SelectCombatAcquisitionCommand(f.Id, Guid.NewGuid(), f.Catalog.Pools[0].PoolId, null, null);
            var pipeline = new TransactionBehavior<SelectCombatAcquisitionCommand, Response<CombatAcquisitionDto>>(db, sync,
                NullLogger<TransactionBehavior<SelectCombatAcquisitionCommand, Response<CombatAcquisitionDto>>>.Instance);
            return await pipeline.Handle(request, ct => handler.Handle(request, ct), Ct);
        }
        var results = await Task.WhenAll(Change(), Change());
        Assert.All(results, x => Assert.True(x.IsSuccess));
        f.Db.ChangeTracker.Clear();
        Assert.Single(await f.Db.InventoryItems.ToListAsync());
        Assert.Single(await f.Db.EconomyLedger.ToListAsync());
        Assert.Null((await f.Progress()).Plain);
        Assert.Equal(3, await f.Db.CombatAcquisitionSelectionReceipts.CountAsync());
    }

    [Fact]
    public async Task Metadata_freezes_commitments_and_dto_exposes_current_counts_without_mutating_queries()
    {
        await using var f = await Fixture.Create();
        var initial = (await f.Service.GetAsync(f.Id, Ct))[0];
        Assert.False(initial!.HasEnteredRegion);
        Assert.False(f.Db.ChangeTracker.HasChanges());
        Assert.NotNull((await f.Service.SelectAsync(f.Id, Guid.NewGuid(), f.Catalog.Pools[0].PoolId, "plain.staff", null, Ct)).Error);
        await f.Process(0, 1, win: false);
        await f.Select("plain.staff", "goblin_mines");
        await f.Process(1, 17);
        var view = (await f.Service.GetAsync(f.Id, Ct))[0];
        var mapper = new MapperConfiguration(x => x.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var dto = mapper.Map<CombatAcquisitionDto>(view);
        Assert.Equal(17, dto.PlainVictories);
        Assert.Equal(31, dto.Targets.Count);
        Assert.Equal(2, dto.Sigils.Count);
        using var metadata = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseNpgsql("Host=localhost;Database=metadata_only;Username=unused").Options);
        var entity = metadata.Model.FindEntityType(typeof(CombatAcquisitionProgress))!;
        Assert.True(entity.FindProperty("Revision")!.IsConcurrencyToken);
        foreach (var (name, value) in new (string, object)[] { ("Plain", (await f.Progress()).Plain!), ("Sigil", (await f.Progress()).Sigil!) })
        {
            var property = entity.FindProperty(name)!;
            Assert.Equal("jsonb", property.GetColumnType());
            var converter = property.GetTypeMapping().Converter!;
            var json = converter.ConvertToProvider(value);
            Assert.Equal(json, converter.ConvertToProvider(converter.ConvertFromProvider(json)));
        }
        var receipt = metadata.Model.FindEntityType(typeof(CombatAcquisitionSelectionReceipt))!;
        Assert.Equal(new[] { "CharacterId", "OperationId" }, receipt.FindPrimaryKey()!.Properties.Select(x => x.Name));
        Assert.Single(receipt.GetForeignKeys());
    }

    private static IEnumerable<InventoryItem> Flatten(CombatAcquisitionRewardOutcome outcome) => outcome.Equipment.Concat(outcome.Sigils);

    [Fact]
    public async Task Real_calculator_includes_ordinary_rewards_and_summary_preserves_frozen_item_identities()
    {
        await using var f = await Fixture.Create(chance: 1);
        var dependencies = new RewardDependencies();
        var calculator = new IdleCombatRewardCalculator(dependencies, dependencies, dependencies, dependencies, dependencies, f.Processor());
        var sessions = new Queue<CombatSession>();
        var awardedIds = new HashSet<Guid>();
        for (var batch = 0; batch < 2; batch++)
        {
            var facts = f.Facts(batch * 240, 240);
            var result = await calculator.CalculateAsync(facts, Ct);
            Assert.Equal(240, result.PowerRewards.Count);
            Assert.Equal(240, result.TotalCinders);
            Assert.Equal(480, result.TotalExperience);
            Assert.Equal(240, result.CraftingRewards.Where(x => x.ItemInstance.ItemBaseId == "soul_dust").Sum(x => x.Quantity));
            foreach (var item in result.PowerRewards) Assert.True(awardedIds.Add(item.ItemInstanceId));
            sessions.Enqueue(new IdleCombatSessionFactory().Create(facts, result));
        }
        var batches = new CombatBatches(sessions);
        var action = new CharacterAction(f.Id, new CombatActionDetails([f.Id], new Area()), Epoch);
        var session = (await new CombatService(batches, batches).PerformIdleCombatAsync(action, Epoch.AddSeconds(4790), Ct))!;
        Assert.Equal(480, session.CombatSummary.RewardBreakdown.PowerItems.Count);
        Assert.Equal(480, session.CombatSummary.TotalCinders);
        Assert.Equal(960, session.CombatSummary.TotalExperience);
        Assert.Equal(awardedIds.Order(), session.CombatSummary.RewardBreakdown.PowerItems.Select(x => x.ItemInstanceId).Order());
        Assert.All(session.CombatSummary.RewardBreakdown.PowerItems, x => Assert.Equal(1, x.Quantity));
    }

    private sealed class RewardDependencies : IBonusService, ILootService, ISoulstoneRewardCalculator,
        IEssenceResonanceService, IAreaExperienceBalanceProvider
    {
        public ValueTask<IReadOnlyDictionary<BonusKind, double>> GetAggregatedAsync(Guid id, DateTimeOffset now, CancellationToken ct = default) =>
            ValueTask.FromResult<IReadOnlyDictionary<BonusKind, double>>(new Dictionary<BonusKind, double>());
        public int Calculate(int durationInSeconds, double dropRatePercent, double doubleDropChancePercent) => 0;
        public decimal GetTargetExperiencePerHour(string areaId) => 720;
        public decimal GetTargetCindersPerHour(string areaId) => 360;
        public int CalculateEncounterExperience(string areaId, int creatureCount) => 2;
        public int CalculateEncounterCinders(string areaId, int creatureCount) => 1;
        public int RandomSigilCalls { get; private set; }
        public IReadOnlyList<InventoryItem> RandomSigils { get; set; } = [];
        public Task<IReadOnlyList<InventoryItem>> RollAsync(Guid id, Area area, int eligibleVictories, CancellationToken ct,
            IReadOnlyDictionary<BonusKind, double>? bonusFactors = null)
        {
            RandomSigilCalls++;
            return Task.FromResult(RandomSigils);
        }
        public Task PrepareEssenceDropsAsync(Guid id, IReadOnlyList<Creature> creatures, bool focus, CancellationToken ct) => Task.CompletedTask;
        public Task<EssenceDropRollResult> RollMonsterEssenceDropAsync(Guid id, string monster, bool eligible, CancellationToken ct,
            EssenceDropRollModifiers? modifiers = null) => throw new NotSupportedException();
        public Task<IReadOnlyList<InventoryItem>> RollEssenceDropsAsync(Guid id, IReadOnlyList<Creature> creatures, bool eligible, CancellationToken ct,
            IReadOnlyDictionary<BonusKind, double>? bonusFactors = null, EssenceDropRollModifiers? modifiers = null) => Task.FromResult<IReadOnlyList<InventoryItem>>([]);
        public Task<IReadOnlyList<IReadOnlyList<InventoryItem>>> GenerateIdleCombatLootBatchAsync(IReadOnlyList<IReadOnlyList<Entity>> groups,
            Dictionary<ItemType, double> multipliers, CancellationToken ct) => Task.FromResult<IReadOnlyList<IReadOnlyList<InventoryItem>>>(
                groups.Select(_ => (IReadOnlyList<InventoryItem>)new[] { new InventoryItem
                {
                    Quantity = 1, ItemInstance = new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = "soul_dust",
                        ItemBase = new ItemBase { Id = "soul_dust", Name = "Existing material", ItemType = ItemType.Resource } }
                } }).ToArray());
        public Task<List<InventoryItem>> GenerateIdleCombatLootAsync(List<Entity> enemies, Dictionary<ItemType, double> multipliers, CancellationToken ct) => throw new NotSupportedException();
        public int GenerateSoulstoneLoot(int seconds) => 0;
        public int GenerateCinderLoot(Dictionary<Guid, int> kills, Dictionary<Guid, int> values, double dropChance = 0.2) => 0;
    }

    private sealed class CombatBatches(Queue<CombatSession> sessions) : ICombatOrchestrationCoordinator, ICombatOutcomeCoordinator
    {
        public Task<CombatSession> ApplyAsync(CombatOutcomeRequest request, CancellationToken ct) => Task.FromResult(sessions.Dequeue());
        public Task<CombatOrchestrationResult> OrchestrateAsync(CombatOrchestrationRequest request, CancellationToken ct)
        {
            var session = sessions.Peek();
            return Task.FromResult(new CombatOrchestrationResult(Guid.NewGuid(), CombatMode.Idle,
                Enumerable.Range(0, session.CombatSummary.TotalBattles).Select(_ => new CombatEncounterRecord(null!, null!)).ToArray(),
                new IdleCombatOrchestrationDetails(session.From, ((IdleCombatOrchestrationRequest)request).Now,
                    session.To, session.CombatSummary.TotalBattles, TimeSpan.FromSeconds(10))));
        }
    }
    private static StarterEquipmentCatalog Equipment() => JsonStarterEquipmentCatalog.Load(Path.Combine(Content(), "equipment-starters.v1.json"));
    private static CombatAcquisitionCatalog Catalog(StarterEquipmentCatalog equipment) => JsonStarterEquipmentCatalog.LoadOrdinary(equipment, Path.Combine(Content(), "equipment-ordinary.v1.json"));
    private static string Content() => Path.Combine(Root(), "LL/src/API/API.LL/Data/equipment");
    private static string Root()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "docs/design/equipment-region-one-inputs.json"))) return directory.FullName;
        throw new DirectoryNotFoundException();
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public DbContextOptions<LLDbContext> DbOptions { get; } = new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        public LLDbContext Db { get; }
        private Fixture() => Db = new(DbOptions);
        public CombatAcquisitionCatalog Catalog { get; private set; } = null!;
        public EquipmentProgressionOptions Flags { get; } = new() { OrdinaryAcquisitionEnabled = true };
        public Character Character { get; private set; } = null!;
        public Guid Id => Character.Id;
        public Actions Actions { get; } = new();
        public Sync Sync { get; } = new();
        public CombatAcquisitionService Service => new(Catalog, new CombatAcquisitionRepository(Db), new QuestRepository(Db), Actions, Sync, Options.Create(Flags), Options.Create(new WorldTowerOptions()));
        public CombatAcquisitionRewardProcessor Processor(CombatAcquisitionCatalog? catalog = null, LLDbContext? db = null) => new(catalog ?? Catalog,
            new CombatAcquisitionRepository(db ?? Db), new ItemBaseRepository(db ?? Db),
            new GameEventOutbox(db ?? Db, new GameEventOutboxConsumerRegistry(), new JsonSerializerOptions(JsonSerializerDefaults.Web), TimeProvider.System), Options.Create(Flags), new PlainEquipmentRepository(db ?? Db));
        public static async Task<Fixture> Create(Guid? id = null, double chance = 0, bool eligible = true)
        {
            var equipment = Equipment();
            var original = CombatAcquisitionTests.Catalog(equipment);
            var f = new Fixture { Catalog = new(equipment, original.Pools.Select(p => p with { DiscoveryChance = chance })) };
            f.Character = new Character { Id = id ?? Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Ordinary", NormalizedName = "ORDINARY", Level = eligible ? 20 : 1 };
            f.Character.Inventory = new Inventory { CharacterId = f.Id };
            f.Db.Characters.Add(f.Character);
            foreach (var option in equipment.Options)
            {
                var definition = equipment.Evaluator.Evaluate(option.DefinitionId, 1, 0, null);
                f.Db.ItemBases.Add(new EquipmentBase { Id = definition.Archetype.ItemBaseId, Name = option.Name, EquipmentType = option.EquipmentType });
            }
            foreach (var sigil in f.Catalog.Pools.SelectMany(p => p.Sigils))
                f.Db.ItemBases.Add(new ItemBase { Id = sigil.ItemBaseId, Name = sigil.FamilyId, IsBound = true, ItemType = ItemType.Resource });
            await f.Db.SaveChangesAsync();
            if (eligible) await f.Unlock();
            return f;
        }
        public async Task Unlock()
        {
            foreach (var sigil in Catalog.Pools[0].Sigils)
                Db.CharacterQuestProgresses.Add(new() { CharacterId = Id, QuestId = sigil.RequiredQuestId!, Status = QuestStatus.Completed });
            await Db.SaveChangesAsync();
        }
        public async Task<CombatAcquisitionProgress> Progress() =>
            (await new CombatAcquisitionRepository(Db).GetAsync(Id, Catalog.Pools[0].PoolId, Ct))!;
        public async Task Select(string? definition, string? family = null, Guid? operation = null) =>
            Assert.Null((await Service.SelectAsync(Id, operation ?? Guid.NewGuid(), Catalog.Pools[0].PoolId, definition, family, Ct)).Error);
        public IdleCombatRewardFacts Facts(int start, int count, bool win = true, string area = "region_01_area_01") => new(Id,
            Epoch.AddSeconds(start * 10), Epoch.AddSeconds((start + count) * 10), Epoch.AddSeconds((start + count) * 10), TimeSpan.FromSeconds(count * 10),
            new Area { Id = area, Name = area }, [Id],
            Enumerable.Range(start, count).Select((value, index) => new IdleEncounterRewardFacts(Guid.NewGuid(), index + 1,
                Epoch.AddSeconds(value * 10), win ? BattleOutcome.Victory : BattleOutcome.Defeat, [], [], null!)).ToArray()) { ScheduleGeneration = 1 };
        public Task<CombatAcquisitionRewardOutcome> Process(int start, int count, bool win = true, string area = "region_01_area_01") => Processor().ProcessAsync(Facts(start, count, win, area), Ct);
        public async Task Settle(CombatAcquisitionRewardOutcome result)
        {
            await new InventoryService(new InventoryRepository(Db)).AddItemsToInventory(Id, Flatten(result).ToList(), "combat-reward", Ct);
            await Db.SaveChangesAsync();
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
    private sealed class Actions : ICharacterActionService
    {
        public CharacterAction? Action { get; set; }
        public Func<Task>? Resolve { get; set; }
        public int ResolveCalls { get; private set; }
        public Task<CharacterAction?> PeekCharacterActionAsync(Guid id, CancellationToken ct) => Task.FromResult(Action);
        public async Task<CharacterAction?> GetCharacterActionAsync(Guid id, CancellationToken ct) { ResolveCalls++; if (Resolve != null) await Resolve(); return Action; }
        public Task<CharacterAction?> StartCharacterActionAsync(CharacterAction action, DateTimeOffset now, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DeleteCharacterActionAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class Sync : IStateSyncService
    {
        public int Invalidations { get; private set; }
        public IReadOnlyDictionary<string, long> GetChangedRevisions(Guid? id) => new Dictionary<string, long>();
        public Task InvalidateCharacterAsync(Guid id, string reason, CancellationToken ct = default) { Invalidations++; return Task.CompletedTask; }
        public Task InvalidateCharacterScopeAsync(Guid id, string scope, string reason, CancellationToken ct = default) { Invalidations++; return Task.CompletedTask; }
        public Task InvalidateWorldScopeAsync(string scope, string reason, CancellationToken ct = default) => Task.CompletedTask;
        public Task<StateSyncCheckpoint> GetCheckpointAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
