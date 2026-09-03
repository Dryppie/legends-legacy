using System.Text.Json;
using API.LiveOps.Hosting;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Administration;
using Application.UseCases.Administration.Queries.GetCompensationEquipmentOptions;
using AutoMapper;
using Domain.Models.Quests;
using Domain.Models.Users;
using Domain.Models.Administration;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.LL.Repositories.Administration;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Persistence.LL.Repositories.Quests;
using Services.LL;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed partial class LiveOpsAdministrationTests
{
    [Fact]
    public async Task EquipmentProgression_standalone_LiveOps_resolves_saved_cohort_and_maps_packaged_equipment_options_with_flags_off()
    {
        await using var db = CreateDb();
        var (_, owner) = AddPlayer(db);
        db.ItemBases.Add(SupportSword());
        db.CharacterQuestProgresses.Add(new CharacterQuestProgress
        {
            CharacterId = owner, QuestId = QuestConstants.SoulArchive, DefinitionVersion = 3,
            Status = QuestStatus.Completed, CreatedAt = Now, UpdatedAt = Now
        });
        await db.SaveChangesAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Content:Root"] = Path.GetFullPath(Path.Combine(TestContentPaths.FindApiRoot(), "..", "API.LiveOps", "bin", "Release", "net10.0", "Data"))
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLiveOpsApplication();
        services.AddLiveOpsServices(configuration);
        services.AddSingleton<IAdministrationRepository>(new AdministrationRepository(db));
        services.AddSingleton<IRefreshTokenRepository>(new RecordingRefreshTokenRepository());
        services.AddSingleton<IItemBaseRepository>(new ItemBaseRepository(db));
        services.AddSingleton<IInventoryRepository>(new InventoryRepository(db));
        services.AddSingleton<IQuestRepository>(new QuestRepository(db));
        services.AddSingleton<IGameEventOutbox>(new NoopGameEventOutbox());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILiveOpsService>();
        var query = new GetCompensationEquipmentOptionsQueryHandler(service, scope.ServiceProvider.GetRequiredService<IMapper>());
        var result = await query.Handle(new(owner, "shortsword"), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.UsesEquipmentProgression);
        Assert.Contains(result.Data.Options, option => option.DefinitionId == "plain.shortsword");
        var blocked = await service.PrepareCompensationGrantAsync(Guid.NewGuid(), owner, "shortsword", 1, CancellationToken.None);
        Assert.False(blocked.IsSuccess);
    }

    private static StarterEquipmentCatalog EquipmentCatalog() => JsonStarterEquipmentCatalog.Load(
        Path.Combine(TestContentPaths.FindApiRoot(), "Data/equipment/equipment-starters.v1.json"));

    private static EquipmentBase SupportSword() => new()
    {
        Id = "shortsword", Name = "Shortsword", Description = "Sword", ItemType = ItemType.Equipment,
        EquipmentType = EquipmentType.OneHanded, Stackable = false, Rarity = Rarity.Common
    };

    [Theory]
    [InlineData("plain.shortsword", null)]
    [InlineData("model_e.r1.goblin_mines.shortsword.blueprint_fury", "blueprint_fury")]
    [InlineData("plain.shortsword", "blueprint_fury")]
    public async Task EquipmentProgression_administrative_grants_preserve_canonical_stats_binding_audit_and_retry_safety(string definition, string? style)
    {
        await using var db = CreateDb();
        var (_, owner) = AddPlayer(db);
        db.Inventories.Add(new Inventory { CharacterId = owner });
        db.ItemBases.Add(SupportSword());
        await db.SaveChangesAsync();
        var catalog = EquipmentCatalog();
        var service = CreateService(db, new RecordingRefreshTokenRepository(), equipmentProgressionCatalog: catalog);
        var operation = Guid.NewGuid();
        var actor = new AdministrationActor("support|one", "Support One");
        var request = new EquipmentGrantRequest(definition, 1, 4, style);
        var prepared = await service.PrepareCompensationGrantAsync(operation, owner, "shortsword", 2, CancellationToken.None, request);
        Assert.True(prepared.IsSuccess);
        Assert.Empty(db.InventoryItems.Local);
        var first = await service.GrantCompensationItemsAsync(operation, owner, actor, "shortsword", 2, "CASE-Progression", null, CancellationToken.None, request);
        Assert.True(first.IsSuccess);
        await db.SaveChangesAsync();
        Assert.Equal(2, first.Value!.GrantedItems.Count);
        Assert.Equal(2, first.Value.GrantedItems.Select(x => x.ItemInstanceId).Distinct().Count());
        Assert.Contains(first.Value.GrantedItems, x => x.ItemInstanceId == prepared.Value!.Equipment!.State.Id);
        Assert.All(first.Value.GrantedItems, item =>
        {
            var equipment = Assert.IsType<EquipmentInstance>(item.ItemInstance);
            var data = Assert.IsType<EquipmentData>(equipment.ProgressionData);
            Assert.Equal(catalog.Evaluator.Evaluate(definition, 1, 4, style).Stats, data.Stats);
            Assert.Equal(owner, data.State.Ownership.OwnerId);
            Assert.Equal(EquipmentOwnershipKind.BoundPersonal, data.State.Ownership.Kind);
            Assert.Equal(EquipmentAwardKind.Administrative, data.State.Provenance.Kind);
            Assert.Equal(operation.ToString("N"), data.State.Provenance.AwardId);
            Assert.False(data.State.Ownership.CanTradeOrDonate);
        });
        Assert.Equal(AdministrationRiskLevel.HighValue, first.Value.Action.RiskLevel);
        using var audit = JsonDocument.Parse(first.Value.Action.DetailsJson);
        Assert.Equal(definition, audit.RootElement.GetProperty("Equipment").GetProperty("DefinitionId").GetString());
        Assert.Equal(2, audit.RootElement.GetProperty("InstanceIds").GetArrayLength());
        var ledgerCount = await db.EconomyLedger.CountAsync();
        Assert.True(ledgerCount > 0);
        db.ChangeTracker.Clear();
        var replay = await service.GrantCompensationItemsAsync(operation, owner, actor, "shortsword", 2, "CASE-Progression", null, CancellationToken.None, request);
        Assert.True(replay.IsSuccess);
        Assert.True(replay.Value!.WasAlreadyProcessed);
        Assert.Empty(replay.Value.GrantedItems);
        Assert.Equal(2, await db.InventoryItems.CountAsync());
        Assert.Equal(ledgerCount, await db.EconomyLedger.CountAsync());
        Assert.Single(await db.AdminActions.ToArrayAsync());
        var conflict = await service.GrantCompensationItemsAsync(operation, owner, actor, "shortsword", 2, "CASE-Progression", null,
            CancellationToken.None, request with { Rank = 5 });
        Assert.False(conflict.IsSuccess);
        Assert.Contains("idempotency", conflict.ErrorCode, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("plain.shortsword", 3, 0, null, 1)]
    [InlineData("plain.shortsword", 1, 6, null, 1)]
    [InlineData("plain.shortsword", 1, -1, null, 1)]
    [InlineData("plain.heavy_helm", 1, 0, null, 1)]
    [InlineData("unknown", 1, 0, null, 1)]
    [InlineData("plain.shortsword", 1, 0, "unknown-style", 1)]
    [InlineData("plain.shortsword", 1, 0, null, 101)]
    public async Task EquipmentProgression_invalid_admin_grants_do_not_write_inventory_or_audit(string definition, int tier, int rank, string? style, int quantity)
    {
        await using var db = CreateDb();
        var (_, owner) = AddPlayer(db);
        db.Inventories.Add(new Inventory { CharacterId = owner });
        db.ItemBases.Add(SupportSword());
        await db.SaveChangesAsync();
        var service = CreateService(db, new RecordingRefreshTokenRepository(), equipmentProgressionCatalog: EquipmentCatalog());
        var result = await service.GrantCompensationItemsAsync(Guid.NewGuid(), owner, new("support|one", "Support One"), "shortsword", quantity,
            "CASE-invalid", null, CancellationToken.None, new(definition, tier, rank, style));
        Assert.False(result.IsSuccess);
        await db.SaveChangesAsync();
        Assert.Empty(await db.InventoryItems.ToArrayAsync());
        Assert.Empty(await db.EconomyLedger.ToArrayAsync());
        Assert.Empty(await db.AdminActions.ToArrayAsync());
    }

    [Theory]
    [InlineData(true)]
    public async Task EquipmentProgression_raw_equipment_requires_canonical_descriptor(bool modern)
    {
        await using var db = CreateDb();
        var (_, owner) = AddPlayer(db);
        db.Inventories.Add(new Inventory { CharacterId = owner });
        db.ItemBases.Add(SupportSword());
        await db.SaveChangesAsync();
        var service = CreateService(db, new RecordingRefreshTokenRepository(), equipmentProgressionCatalog: EquipmentCatalog());
        var result = await service.GrantCompensationItemsAsync(Guid.NewGuid(), owner, new("support|one", "Support One"), "shortsword", 1,
            "CASE-raw", null, CancellationToken.None);
        Assert.Equal(!modern, result.IsSuccess);
        if (!modern) Assert.Null(Assert.IsType<EquipmentInstance>(Assert.Single(result.Value!.GrantedItems).ItemInstance).ProgressionData);
        else Assert.Empty(db.InventoryItems.Local);
        var options = await service.GetCompensationEquipmentOptionsAsync(owner, "shortsword", CancellationToken.None);
        Assert.Equal(modern, options.UsesEquipmentProgression);
        if (modern) Assert.Contains(options.Options, x => x.DefinitionId == "plain.shortsword" && x.MaximumTier == 2);
        else Assert.Empty(options.Options);
    }

    [Fact]
    public async Task EquipmentProgression_compensation_grants_supported_items()
    {
        const string id = "item.monster_core.lesser";
        await using var db = CreateDb();
        var (_, owner) = AddPlayer(db);
        db.Inventories.Add(new Inventory { CharacterId = owner });
        db.ItemBases.Add(new ItemBase { Id = id, Name = id, Description = "Resource", ItemType = ItemType.Resource, Stackable = true });
        await db.SaveChangesAsync();
        var service = CreateService(db, new RecordingRefreshTokenRepository(), equipmentProgressionCatalog: EquipmentCatalog());
        var result = await service.GrantCompensationItemsAsync(Guid.NewGuid(), owner, new("support|one", "Support One"), id, 3,
            "CASE-resource", null, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(id, Assert.Single(result.Value!.GrantedItems).ItemInstance.ItemBaseId);
    }
}
