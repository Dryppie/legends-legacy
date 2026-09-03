using Application.Interfaces.Services.LL;
using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Behaviors;
using Application.UseCases.Equipments.Commands.ClaimStarterEquipment;
using Application.UseCases.Equipments.Dtos;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Quests;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Persistence.LL.Repositories.Quests;
using Services.LL.Combat;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Items;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class EquipmentIntegrationTests
{
    [Fact]
    public async Task Access_read_reports_disabled_gated_and_claimed_stages_without_granting_items()
    {
        await using var db = CreateDb();
        var characterId = await Seed(db);
        var writer = new RepositoryRewardWriter(db);
        var disabled = await Service(db, writer, false).GetAccessAsync(characterId, CancellationToken.None);
        Assert.False(disabled.StarterAcquisitionEnabled);
        Assert.Empty(disabled.Starters);
        var service = Service(db, writer);
        var access = await service.GetAccessAsync(characterId, CancellationToken.None);
        Assert.True(access.Starters.Single(x => x.Kind == StarterEquipmentGrantKind.FirstWeapon).CanClaim);
        Assert.False(access.Starters.Single(x => x.Kind == StarterEquipmentGrantKind.ReadyForRoad).CanClaim);
        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Equal(0, writer.Calls);
        var claim = await service.ClaimAsync(characterId, StarterEquipmentGrantKind.FirstWeapon, StaffKit, CancellationToken.None);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var claimed = (await service.GetAccessAsync(characterId, CancellationToken.None)).Starters.Single(x => x.Kind == StarterEquipmentGrantKind.FirstWeapon);
        Assert.False(claimed.CanClaim);
        Assert.Equal(claim.Grant!.Equipment.Select(x => x.State.Id), claimed.Grant!.Equipment.Select(x => x.State.Id));
        Assert.Equal(1, writer.Calls);
        Assert.False(db.ChangeTracker.HasChanges());
        var mapper = new MapperConfiguration(config => config.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var dto = mapper.Map<EquipmentAccessDto>(await service.GetAccessAsync(characterId, CancellationToken.None));
        Assert.Equal(StaffKit.Order(), dto.Starters.Single(x => x.Kind == StarterEquipmentGrantKind.FirstWeapon).Grant!.DefinitionIds.Order());
        var option = mapper.Map<StarterEquipmentOptionDto>(Catalog().Options.Single(x => x.DefinitionId == "plain.staff"));
        Assert.NotEmpty(option.Stats);
        Assert.Equal(Catalog().Evaluator.Evaluate("plain.staff", 1, 0, null).Stats, option.Stats);
    }

    private static readonly string[] StaffKit = ["plain.staff", "plain.heavy_helm", "plain.light_vest", "plain.cloth_pants"];
    private static StarterEquipmentCatalog Catalog() => JsonStarterEquipmentCatalog.Load(ContentPath());

    [Fact]
    public void Application_mapping_exposes_starter_options_and_model_e_metadata()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var catalog = Catalog();
        Assert.Equal(31, mapper.Map<List<StarterEquipmentOptionDto>>(catalog.Options).Count);
        var equipment = Instance(EquipmentData.Create(Award(catalog, Guid.NewGuid()), catalog.Evaluator));
        var dto = mapper.Map<EquipmentInstanceDto>(equipment);
        Assert.True(dto.IsBound);
        Assert.NotNull(dto.Progression);
        Assert.Equal("plain.staff", dto.Progression.DefinitionId);
        Assert.Equal(0, dto.Progression.Rank);
        Assert.Equal(EquipmentOwnershipKind.BoundPersonal, dto.Progression.Ownership);
        Assert.Null(dto.CraftingDesign);
        Assert.Null(dto.Potential);
        Assert.Empty(dto.BaseModifiers);
    }

    [Fact]
    public void Frozen_json_preserves_authored_state_and_rejects_unsupported_or_corrupt_state()
    {
        var catalog = Catalog();
        var state = EquipmentState.Restore(
            Award(catalog, Guid.NewGuid(), EquipmentAwardKind.RandomDiscovery).ToSnapshot() with { Rank = 3 });
        var descriptor = EquipmentData.Create(state, catalog.Evaluator);
        var restored = EquipmentData.Deserialize(descriptor.Serialize());
        Assert.Equal(descriptor.Serialize(), restored.Serialize());
        Assert.Equal(3, restored.EquipmentState.Rank);
        Assert.Throws<InvalidOperationException>(() => EquipmentState.Restore(state.ToSnapshot() with { ModelVersion = 999 }));
        Assert.Throws<InvalidOperationException>(() => EquipmentState.Restore(state.ToSnapshot() with { Rank = 6 }));
        Assert.Throws<InvalidOperationException>(() => EquipmentState.Restore(state.ToSnapshot() with
        {
            Provenance = new(EquipmentAwardKind.QuestReward, "quest.test", "award.test"),
            Ownership = new(EquipmentOwnershipKind.UnboundPersonal, state.Ownership.OwnerId)
        }));
    }

    [Fact]
    public async Task Descriptor_and_snapshot_roundtrip_keep_frozen_stats_after_live_item_changes()
    {
        await using var db = CreateDb();
        var catalog = Catalog();
        var characterId = Guid.NewGuid();
        var state = Award(catalog, characterId);
        var instance = Instance(EquipmentData.Create(state, catalog.Evaluator));
        var snapshot = new CharacterSnapshot { Id = Guid.NewGuid(), CharacterId = characterId, Name = "Frozen", Level = 1,
            Equipment = [EquipmentSnapshot.From(EquipmentSlotType.MainHand, instance), EquipmentSnapshot.From(EquipmentSlotType.OffHand, instance)] };
        var original = instance.ProgressionData!.Serialize();
        db.ItemInstances.Add(instance);
        db.CharacterSnapshots.Add(snapshot);
        await db.SaveChangesAsync();
        instance.ApplyProgressionData(EquipmentData.Create(
            EquipmentState.Restore(state.ToSnapshot() with { Rank = 1 }), catalog.Evaluator));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var live = await db.ItemInstances.OfType<EquipmentInstance>().Include(x => x.ItemBase).SingleAsync();
        var persisted = await db.CharacterSnapshots.Include(x => x.Equipment).SingleAsync();
        Assert.Equal(1, live.ProgressionData!.State.Rank);
        Assert.All(persisted.Equipment, x => Assert.Equal(original, x.ProgressionData!.Serialize()));
        Assert.Empty(live.BaseModifiers);
        Assert.True(live.UsesProgressionNormalizedRatings);
        Assert.Null(live.Potential);
        var participant = Assert.Single(await new SnapshotCombatantBuilder(db, new CombatSetupService(null!, null!, null!, null!))
            .BuildAsync([new SnapshotCombatantRequest(persisted, new CombatParticipantSlot(characterId.ToString(), characterId, CombatSide.Friendly, 1))], CancellationToken.None));
        var frozen = Assert.Single(participant.Combatant.Equipment);
        Assert.Equal(original, frozen.ProgressionData!.Serialize());
        Assert.Equal("Magical", participant.Combatant.MainHandEquipment!.ProgressionData!.Behavior.AttackCategory);
        Assert.Equal("Ranged", frozen.ProgressionData.Behavior.RangeCategory);
        Assert.Equal(EquipmentData.Deserialize(original).Stats.OrderBy(x => x.Key),
            frozen.AttributeModifiers.ToDictionary(x => x.AttributeType, x => x.Amount).OrderBy(x => x.Key));
    }

    [Fact]
    public void Relational_model_uses_jsonb_and_unique_character_stage_entitlement()
    {
        using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=model_metadata_only;Username=unused").Options);
        foreach (var type in new[] { typeof(EquipmentInstance), typeof(EquipmentSnapshot) })
        {
            var property = db.Model.FindEntityType(type)!.FindProperty(nameof(EquipmentInstance.ProgressionData))!;
            Assert.Equal("jsonb", property.GetColumnType());
            var data = EquipmentData.Create(Award(Catalog(), Guid.NewGuid()), Catalog().Evaluator);
            var converter = property.GetTypeMapping().Converter!;
            Assert.Equal(data.Serialize(), Assert.IsType<EquipmentData>(converter.ConvertFromProvider(converter.ConvertToProvider(data))).Serialize());
        }
        Assert.Equal(["CharacterId", "Kind"], db.Model.FindEntityType(typeof(StarterEquipmentGrant))!.FindPrimaryKey()!.Properties.Select(x => x.Name));
    }

    [Theory]
    [InlineData("plain.staff", null)]
    [InlineData("plain.shortsword", "plain.towershield")]
    [InlineData("plain.dagger", "plain.dagger")]
    public async Task Starter_claim_reload_equip_and_retry_grant_exactly_one_chosen_kit(string main, string? off)
    {
        await using var db = CreateDb();
        var characterId = await Seed(db);
        var writer = new RepositoryRewardWriter(db);
        var service = Service(db, writer);
        var selection = off is null ? new[] { main, StaffKit[1], StaffKit[2], StaffKit[3] }
            : new[] { main, off, StaffKit[1], StaffKit[2], StaffKit[3] };
        var result = await service.ClaimAsync(characterId, StarterEquipmentGrantKind.FirstWeapon, selection, CancellationToken.None);
        Assert.Null(result.Error);
        var sameTransactionReplay = await service.ClaimAsync(characterId, StarterEquipmentGrantKind.FirstWeapon, selection, CancellationToken.None);
        Assert.Same(result.Grant, sameTransactionReplay.Grant);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var awarded = await db.ItemInstances.OfType<EquipmentInstance>().Include(x => x.ItemBase).ToListAsync();
        Assert.Equal(selection.Length, awarded.Count);
        Assert.All(awarded, x => { Assert.True(x.IsBound); Assert.Null(x.BaseRecipeId); });
        var repository = new EquipmentSlotRepository(db);
        foreach (var equipment in awarded.OrderBy(x => x.EquipmentBase.EquipmentType == EquipmentType.OffHand))
            Assert.True((await repository.EquipEquipmentAsync(characterId, equipment.Id, null, CancellationToken.None)).Succeeded);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var replay = await service.ClaimAsync(characterId, StarterEquipmentGrantKind.FirstWeapon, selection.Reverse().ToArray(), CancellationToken.None);
        Assert.Equal(result.Grant!.Equipment.Select(x => x.State.Id), replay.Grant!.Equipment.Select(x => x.State.Id));
        Assert.Equal(1, writer.Calls);
        Assert.Empty(await db.InventoryItems.ToListAsync());
        var slots = await repository.GetEquipmentSlotsByEntityIdAsync(characterId, CancellationToken.None);
        Assert.Equal(5, slots.Count(x => x.EquipmentInstanceId != null));
        Assert.Equal(selection.Length, slots.Where(x => x.EquipmentInstanceId != null).Select(x => x.EquipmentInstanceId).Distinct().Count());
        var conflict = await service.ClaimAsync(characterId, StarterEquipmentGrantKind.FirstWeapon, ["plain.maul", StaffKit[1], StaffKit[2], StaffKit[3]], CancellationToken.None);
        Assert.Null(conflict.Grant);
    }

    [Fact]
    public async Task Accessory_stage_has_its_own_prerequisite_and_once_only_receipt()
    {
        await using var db = CreateDb();
        var characterId = await Seed(db);
        var writer = new RepositoryRewardWriter(db);
        var service = Service(db, writer);
        Assert.Null((await service.ClaimAsync(characterId, StarterEquipmentGrantKind.ReadyForRoad, [], CancellationToken.None)).Grant);
        db.CharacterQuestProgresses.Add(new() { CharacterId = characterId, QuestId = "quest.onboarding.first_weapon", Status = QuestStatus.Completed });
        await db.SaveChangesAsync();
        var result = await service.ClaimAsync(characterId, StarterEquipmentGrantKind.ReadyForRoad, [], CancellationToken.None);
        await db.SaveChangesAsync();
        Assert.Equal(new[] { "amulet", "band", "vial" }, result.Grant!.Equipment.Select(x => x.ItemBaseId).Order());
        Assert.NotNull((await service.ClaimAsync(characterId, StarterEquipmentGrantKind.ReadyForRoad, [], CancellationToken.None)).Grant);
        Assert.Equal(1, writer.Calls);
    }

    [Fact]
    public async Task Disabled_missing_prerequisite_and_invalid_choices_have_no_writes()
    {
        await using var db = CreateDb();
        var characterId = await Seed(db, completeSoulArchive: false);
        var writer = new RepositoryRewardWriter(db);
        var disabled = Service(db, writer, enabled: false);
        Assert.Empty(disabled.GetOptions());
        Assert.Null((await disabled.ClaimAsync(characterId, StarterEquipmentGrantKind.FirstWeapon, StaffKit, CancellationToken.None)).Grant);
        var service = Service(db, writer);
        Assert.Equal(31, service.GetOptions().Count);
        Assert.Null((await service.ClaimAsync(characterId, StarterEquipmentGrantKind.FirstWeapon, StaffKit, CancellationToken.None)).Grant);
        foreach (var invalid in new[] { new[] { "unknown" }, new[] { "plain.towershield", "plain.grimoire", StaffKit[1], StaffKit[2], StaffKit[3] },
            new[] { "plain.staff", "plain.dagger", StaffKit[1], StaffKit[2], StaffKit[3] } })
            Assert.Null((await service.ClaimAsync(characterId, StarterEquipmentGrantKind.FirstWeapon, invalid, CancellationToken.None)).Grant);
        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Equal(0, writer.Calls);
    }

    [Fact]
    public async Task Command_pipeline_serializes_simultaneous_claims_from_separate_contexts()
    {
        var options = DbOptions();
        await using var seed = new LLDbContext(options);
        var characterId = await Seed(seed);
        var request = new ClaimStarterEquipmentCommand(characterId, StarterEquipmentGrantKind.FirstWeapon, StaffKit);
        var mapper = new MapperConfiguration(config => config.AddProfile<TestProfile>(), NullLoggerFactory.Instance).CreateMapper();
        async Task<Response<StarterEquipmentGrantDto>> Claim()
        {
            await using var db = new LLDbContext(options);
            var handler = new ClaimStarterEquipmentCommandHandler(Service(db, new RepositoryRewardWriter(db)), mapper);
            var pipeline = new TransactionBehavior<ClaimStarterEquipmentCommand, Response<StarterEquipmentGrantDto>>(
                db, new NoopStateSync(), NullLogger<TransactionBehavior<ClaimStarterEquipmentCommand, Response<StarterEquipmentGrantDto>>>.Instance);
            return await pipeline.Handle(request, ct => handler.Handle(request, ct), CancellationToken.None);
        }
        await Task.WhenAll(Claim(), Claim());
        await using var verify = new LLDbContext(options);
        Assert.Single(await verify.StarterEquipmentGrants.ToListAsync());
        Assert.Equal(4, await verify.InventoryItems.CountAsync());
        Assert.Equal(4, await verify.EconomyLedger.CountAsync());
    }

    [Fact]
    public async Task Equip_binds_discoveries_and_current_equipment_rejects_legacy_tempering()
    {
        await using var db = CreateDb();
        var characterId = await Seed(db);
        var catalog = Catalog();
        var equipment = Instance(EquipmentData.Create(Award(catalog, characterId, EquipmentAwardKind.RandomDiscovery), catalog.Evaluator));
        equipment.ItemBase = await db.ItemBases.SingleAsync(x => x.Id == equipment.ItemBaseId);
        db.InventoryItems.Add(new() { InventoryId = characterId, ItemInstanceId = equipment.Id, ItemInstance = equipment });
        await db.SaveChangesAsync();
        Assert.False(equipment.IsBound);
        var repository = new EquipmentSlotRepository(db);
        Assert.True((await repository.EquipEquipmentAsync(characterId, equipment.Id, null, CancellationToken.None)).Succeeded);
        await db.SaveChangesAsync();
        Assert.True(await repository.UnequipEquipmentAsync(characterId, EquipmentSlotType.MainHand, CancellationToken.None));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var persisted = await db.ItemInstances.OfType<EquipmentInstance>().Include(x => x.ItemBase).SingleAsync();
        Assert.True(persisted.IsBound);
        Assert.Throws<InvalidOperationException>(() => persisted.BindEquipmentProgressionForEquip(Guid.NewGuid()));
        Assert.Single(await db.InventoryItems.ToListAsync());
        Assert.Throws<InvalidOperationException>(() => new TemperingMechanicsService().ApplyTemperingAttempt(persisted, null!, new Random(1)));
    }

    private static EquipmentState Award(StarterEquipmentCatalog catalog, Guid owner, EquipmentAwardKind kind = EquipmentAwardKind.QuestReward) =>
        EquipmentState.Award(Guid.NewGuid(), catalog.Evaluator, "plain.staff", 1, 0,
            new(kind, "test", Guid.NewGuid().ToString()), new(EquipmentOwnershipKind.UnboundPersonal, owner));

    private static EquipmentInstance Instance(EquipmentData data)
    {
        var instance = new EquipmentInstance { Id = data.State.Id, ItemBaseId = data.ItemBaseId,
            ItemBase = new EquipmentBase { Id = data.ItemBaseId, Name = data.DisplayName, EquipmentType = data.EquipmentType } };
        instance.ApplyProgressionData(data);
        return instance;
    }

    private static DbContextOptions<LLDbContext> DbOptions() => new DbContextOptionsBuilder<LLDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
    private static LLDbContext CreateDb() => new(DbOptions());
    private static StarterEquipmentService Service(LLDbContext db, ILootRewardWriter writer, bool enabled = true) => new(Catalog(),
        new StarterEquipmentRepository(db), new QuestRepository(db), new ItemBaseRepository(db), writer,
        Options.Create(new EquipmentProgressionOptions { StarterAcquisitionEnabled = enabled }));

    private static async Task<Guid> Seed(LLDbContext db, bool completeSoulArchive = true)
    {
        var id = Guid.NewGuid();
        db.Characters.Add(new Character { Id = id, UserId = Guid.NewGuid(), Name = "Starter", NormalizedName = "STARTER", Level = 1,
            Inventory = new Inventory { CharacterId = id }, EquipmentSlots = Enum.GetValues<EquipmentSlotType>()
                .Select(type => new EquipmentSlot { EntityId = id, EquipmentSlotType = type }).ToList() });
        var catalog = Catalog();
        foreach (var option in catalog.Options)
        {
            var data = catalog.Evaluator.Evaluate(option.DefinitionId, 1, 0, null);
            db.ItemBases.Add(new EquipmentBase { Id = data.Archetype.ItemBaseId, Name = option.Name, EquipmentType = option.EquipmentType });
        }
        if (completeSoulArchive) db.CharacterQuestProgresses.Add(new() { CharacterId = id, QuestId = "quest.onboarding.soul_archive", Status = QuestStatus.Completed });
        await db.SaveChangesAsync();
        return id;
    }

    private sealed class RepositoryRewardWriter(LLDbContext db) : ILootRewardWriter
    {
        public int Calls { get; private set; }
        public async Task AddLootAsync(Guid id, IReadOnlyCollection<InventoryItem> items, string source, string? location, CancellationToken ct)
        {
            Calls++;
            await Task.Yield();
            await new InventoryRepository(db).AddItemsToInventory(id, items.ToList(), source, ct);
        }
    }

    private sealed class TestProfile : Profile
    {
        public TestProfile() { new StarterEquipmentGrantDto().Mapping(this); new EquipmentDto().Mapping(this); }
    }
    private sealed class NoopStateSync : IStateSyncService
    {
        public IReadOnlyDictionary<string, long> GetChangedRevisions(Guid? characterId) => new Dictionary<string, long>();
        public Task InvalidateCharacterAsync(Guid characterId, string reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateCharacterScopeAsync(Guid characterId, string scope, string reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateWorldScopeAsync(string scope, string reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<StateSyncCheckpoint> GetCheckpointAsync(Guid characterId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static string ContentPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "LL/src/API/API.LL/Data/equipment/equipment-starters.v1.json");
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("Equipment progression starter catalog not found.");
    }
}
