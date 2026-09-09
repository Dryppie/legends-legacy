using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class EquipmentUpgradeExecutionTests
{
    [Theory]
    [InlineData(EquipmentUpgradeOperationKind.Reinforce)]
    [InlineData(EquipmentUpgradeOperationKind.Dismantle)]
    [InlineData(EquipmentUpgradeOperationKind.ApplyVariant)]
    public async Task Actions_execute_without_a_preview_and_retries_apply_only_once(EquipmentUpgradeOperationKind kind)
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.Request(kind);
        var operationId = Guid.NewGuid();

        var result = await fixture.Service.ExecuteAsync(fixture.Character.Id, operationId, request, default);
        Assert.NotNull(result.Outcome);
        Assert.Null(result.Error);
        await fixture.Db.SaveChangesAsync();
        var cinders = fixture.Character.Cinders;
        var parts = fixture.Parts.Quantity;
        var ledgerCount = await fixture.Db.EconomyLedger.CountAsync();

        var retry = await fixture.Service.ExecuteAsync(fixture.Character.Id, operationId, request, default);
        Assert.Equal(result.Outcome, retry.Outcome);
        Assert.Equal(cinders, fixture.Character.Cinders);
        Assert.Equal(parts, fixture.Parts.Quantity);
        Assert.Equal(ledgerCount, await fixture.Db.EconomyLedger.CountAsync());
        Assert.Single(await fixture.Db.EquipmentUpgradeReceipts.ToListAsync());

        var reused = await fixture.Service.ExecuteAsync(fixture.Character.Id, operationId,
            request with { ItemInstanceId = Guid.NewGuid() }, default);
        Assert.Null(reused.Outcome);
        Assert.Equal("This operation ID has already been used for a different request.", reused.Error);
    }

    [Theory]
    [InlineData(EquipmentUpgradeOperationKind.Reinforce)]
    [InlineData(EquipmentUpgradeOperationKind.Dismantle)]
    [InlineData(EquipmentUpgradeOperationKind.ApplyVariant)]
    public async Task Balance_changes_and_elapsed_time_do_not_invalidate_actions(EquipmentUpgradeOperationKind kind)
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.Request(kind);
        var preview = await fixture.Service.PreviewAsync(fixture.Character.Id, request, default);
        Assert.True(preview.CanExecute, preview.UnavailableReason);
        fixture.Character.Cinders += 123;
        fixture.Parts.Quantity += 7;
        fixture.Clock.Now += TimeSpan.FromDays(1);
        var cinders = fixture.Character.Cinders;
        var parts = fixture.Parts.Quantity;

        var result = await fixture.Service.ExecuteAsync(fixture.Character.Id, preview.OperationId, request, default);

        Assert.NotNull(result.Outcome);
        Assert.Equal(cinders - preview.CinderCost, fixture.Character.Cinders);
        Assert.Equal(parts - preview.PartsCost + preview.PartsReturned, fixture.Parts.Quantity);
        Assert.Equal(fixture.Clock.Now, result.Outcome.OccurredAtUtc);
    }

    [Theory]
    [InlineData(EquipmentUpgradeOperationKind.Reinforce, "cinders", "Not enough Cinders.")]
    [InlineData(EquipmentUpgradeOperationKind.Reinforce, "parts", "Not enough Reinforcement Parts.")]
    [InlineData(EquipmentUpgradeOperationKind.ApplyVariant, "cinders", "Not enough Cinders.")]
    [InlineData(EquipmentUpgradeOperationKind.ApplyVariant, "blueprints", "You need one matching blueprint.")]
    public async Task Actions_recheck_current_resources_and_reject_without_payment(
        EquipmentUpgradeOperationKind kind, string resource, string expectedError)
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.Request(kind);
        var preview = await fixture.Service.PreviewAsync(fixture.Character.Id, request, default);
        Assert.True(preview.CanExecute, preview.UnavailableReason);
        if (resource == "cinders") fixture.Character.Cinders = 0;
        if (resource == "parts") fixture.Parts.Quantity = 0;
        if (resource == "blueprints") fixture.Blueprint.Quantity = 0;
        var before = fixture.Equipment.ProgressionData!.Serialize();
        var cinders = fixture.Character.Cinders;
        var parts = fixture.Parts.Quantity;
        var blueprints = fixture.Blueprint.Quantity;

        var result = await fixture.Service.ExecuteAsync(fixture.Character.Id, preview.OperationId, request, default);

        Assert.Null(result.Outcome);
        Assert.Equal(expectedError, result.Error);
        Assert.Equal(before, fixture.Equipment.ProgressionData.Serialize());
        Assert.Equal(cinders, fixture.Character.Cinders);
        Assert.Equal(parts, fixture.Parts.Quantity);
        Assert.Equal(blueprints, fixture.Blueprint.Quantity);
        Assert.Empty(fixture.Db.EquipmentUpgradeReceipts.Local);
        Assert.Empty(fixture.Db.EconomyLedger.Local);
    }

    [Fact]
    public async Task Reinforcement_uses_the_current_rank_and_cost()
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.Request(EquipmentUpgradeOperationKind.Reinforce);
        var preview = await fixture.Service.PreviewAsync(fixture.Character.Id, request, default);
        fixture.Equipment.ApplyProgressionData(preview.After!);
        var current = await fixture.Service.PreviewAsync(fixture.Character.Id, request, default);

        var result = await fixture.Service.ExecuteAsync(fixture.Character.Id, preview.OperationId, request, default);

        Assert.NotNull(result.Outcome);
        Assert.Equal(2, result.Outcome.After!.State.Rank);
        Assert.Equal(current.PartsCost, result.Outcome.PartsSpent);
        Assert.Equal(current.CinderCost, result.Outcome.CindersSpent);
        Assert.Equal(1_000_000 - current.CinderCost, fixture.Character.Cinders);
    }

    [Fact]
    public async Task Dismantling_rechecks_favorites_and_requires_explicit_confirmation()
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.Request(EquipmentUpgradeOperationKind.Dismantle);
        var preview = await fixture.Service.PreviewAsync(fixture.Character.Id, request, default);
        fixture.InventoryEquipment.IsFavorite = true;

        var rejected = await fixture.Service.ExecuteAsync(fixture.Character.Id, preview.OperationId, request, default);
        Assert.Null(rejected.Outcome);
        Assert.Equal("Confirm dismantling this favorite item explicitly.", rejected.Error);
        Assert.Empty(fixture.Db.EquipmentUpgradeReceipts.Local);

        var confirmed = await fixture.Service.ExecuteAsync(fixture.Character.Id, preview.OperationId,
            request with { AllowFavoriteDismantle = true }, default);
        Assert.NotNull(confirmed.Outcome);
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.Parse("2026-09-09T12:04:59Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public LLDbContext Db { get; } = new(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public Character Character { get; } = new() { Id = Guid.NewGuid(), Cinders = 1_000_000 };
        public Clock Clock { get; } = new();
        public EquipmentInstance Equipment { get; private set; } = null!;
        public InventoryItem InventoryEquipment { get; private set; } = null!;
        public InventoryItem Parts { get; private set; } = null!;
        public InventoryItem Blueprint { get; private set; } = null!;
        public EquipmentUpgradeService Service { get; private set; } = null!;

        public EquipmentUpgradeRequest Request(EquipmentUpgradeOperationKind kind) =>
            new(kind, Equipment.Id, BlueprintStyleId: kind == EquipmentUpgradeOperationKind.ApplyVariant ? "blueprint_fury" : null);

        public static async Task<Fixture> Create()
        {
            var fixture = new Fixture();
            var root = Path.Combine(TestContentPaths.FindApiRoot(), "Data", "equipment");
            var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(root, "equipment-starters.v1.json"));
            var blueprints = JsonEquipmentBlueprintCatalog.Load(Path.Combine(root, "equipment-blueprints.v1.json"), catalog);
            var prices = JsonEquipmentUpgradePrices.Load(Path.Combine(root, "equipment-upgrades.v1.json"));
            var state = EquipmentState.Award(Guid.NewGuid(), catalog.Evaluator, "plain.shortsword", 1, 0,
                new(EquipmentAwardKind.RandomDiscovery, "test", "test"),
                new(EquipmentOwnershipKind.UnboundPersonal, fixture.Character.Id));
            var itemBase = new EquipmentBase { Id = "shortsword", Name = "Shortsword", EquipmentType = EquipmentType.OneHanded };
            fixture.Equipment = new EquipmentInstance { Id = state.Id, ItemBaseId = itemBase.Id, ItemBase = itemBase };
            fixture.Equipment.ApplyProgressionData(EquipmentData.Create(state, catalog.Evaluator));
            fixture.InventoryEquipment = fixture.Add(fixture.Equipment, 1);
            fixture.Parts = fixture.Add(new ItemBase
            {
                Id = EquipmentKeys.ReinforcementPartsItemBaseId, Name = "Reinforcement Parts", Stackable = true, IsBound = true
            }, 1000);
            fixture.Blueprint = fixture.Add(new ItemBase { Id = "item.blueprint_fury", Name = "Blueprint: Fury", Stackable = true }, 2);
            fixture.Db.Characters.Add(fixture.Character);
            await fixture.Db.SaveChangesAsync();
            fixture.Service = new EquipmentUpgradeService(catalog, prices,
                new EquipmentUpgradeRepository(fixture.Db, blueprints), null!, null!, fixture.Clock, null!, blueprints);
            return fixture;
        }

        private InventoryItem Add(ItemBase itemBase, int quantity) => Add(
            new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = itemBase.Id, ItemBase = itemBase }, quantity);

        private InventoryItem Add(ItemInstance item, int quantity)
        {
            var row = new InventoryItem { InventoryId = Character.Id, ItemInstanceId = item.Id, ItemInstance = item, Quantity = quantity };
            Db.InventoryItems.Add(row);
            return row;
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
