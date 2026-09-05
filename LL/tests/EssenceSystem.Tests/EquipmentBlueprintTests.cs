using System.Text.Json;
using Application.UseCases.Inventories.SelectionCrates;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Items;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;

namespace EssenceSystem.Tests;

public sealed class EquipmentBlueprintTests
{
    private static string Root => Path.Combine(TestContentPaths.FindApiRoot(), "Data", "equipment");
    private static StarterEquipmentCatalog Equipment() => JsonStarterEquipmentCatalog.Load(Path.Combine(Root, "equipment-starters.v1.json"));
    private static EquipmentBlueprintCatalog Blueprints(StarterEquipmentCatalog equipment) =>
        JsonEquipmentBlueprintCatalog.Load(Path.Combine(Root, "equipment-blueprints.v1.json"), equipment);

    [Fact]
    public void Every_consumable_blueprint_is_authored_as_rare()
    {
        var catalog = Blueprints(Equipment());
        using var items = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContentPaths.FindApiRoot(), "Data", "items", "items.json")));
        var rarities = items.RootElement.EnumerateArray().ToDictionary(
            item => item.GetProperty("id").GetString()!,
            item => item.GetProperty("rarity").GetString()!);

        Assert.All(catalog.Blueprints, blueprint =>
            Assert.Equal("Rare", rarities[blueprint.ItemId]));
    }

    [Fact]
    public void Legacy_blueprint_selection_boxes_award_current_consumable_blueprints()
    {
        var container = SelectionContainerCatalog.Find(
            LegacyBlueprintSelectionBoxCatalog.ItemBaseId,
            Blueprints(Equipment()));

        Assert.NotNull(container);
        Assert.Equal(11, container.Options.Count);
        Assert.All(container.Options, option =>
            Assert.StartsWith("item.blueprint_", option.ItemId));
    }

    [Fact]
    public void Every_compatible_variant_preserves_base_stats_at_all_released_tiers_rarities_and_ranks()
    {
        var equipment = Equipment();
        foreach (var definition in equipment.Evaluator.Definitions.Where(x => x.NativeStyleId is null))
        foreach (var tier in new[] { 1, 2 })
        foreach (var rank in new[] { 0, 5 })
        {
            var baseline = equipment.Evaluator.Evaluate(definition.Id, tier, rank, null, ItemQuality.Masterpiece, 1.05);
            foreach (var style in equipment.Styles.Where(x => x.CompatibleArchetypeIds.Contains(definition.ArchetypeId)))
            {
                var variant = equipment.Evaluator.Evaluate(definition.Id, tier, rank, style.Id, ItemQuality.Masterpiece, 1.05);
                Assert.All(baseline.Stats, stat => Assert.True(variant.Stats.GetValueOrDefault(stat.Key) >= stat.Value,
                    $"{definition.Id}/{style.Id}/{tier}/{rank}: {stat.Key} decreased"));
                Assert.Equal(baseline.TargetBudget * 1.15, variant.TargetBudget, 6);
            }
        }
    }

    [Fact]
    public void Conversion_and_reinforcement_commute_and_replacement_does_not_stack_bonuses()
    {
        var equipment = Equipment();
        var state = Award(equipment);
        var convertedFirst = state.ApplyVariant(equipment.Evaluator, "blueprint_fury").Reinforce(equipment.Evaluator);
        var reinforcedFirst = state.Reinforce(equipment.Evaluator).ApplyVariant(equipment.Evaluator, "blueprint_fury");
        Assert.Equal(EquipmentData.Create(convertedFirst, equipment.Evaluator).Serialize(),
            EquipmentData.Create(reinforcedFirst, equipment.Evaluator).Serialize());
        var replaced = convertedFirst.ApplyVariant(equipment.Evaluator, "blueprint_arcane");
        var direct = state.Reinforce(equipment.Evaluator).ApplyVariant(equipment.Evaluator, "blueprint_arcane");
        Assert.Equal(EquipmentData.Create(direct, equipment.Evaluator).Serialize(), EquipmentData.Create(replaced, equipment.Evaluator).Serialize());
        Assert.Equal(state.Quality, replaced.Quality);
        Assert.Equal(state.AttributeRollMultiplier, replaced.AttributeRollMultiplier);
        Assert.Throws<InvalidOperationException>(() => replaced.ApplyVariant(equipment.Evaluator, "blueprint_arcane"));
        Assert.Throws<ArgumentException>(() => state.ApplyVariant(equipment.Evaluator, "missing"));
        Assert.Equal(state.Ownership, state.ApplyVariant(equipment.Evaluator, "blueprint_fury").Ownership);
    }

    [Fact]
    public void Old_frozen_variants_keep_their_allocation_when_loaded_or_reinforced()
    {
        var equipment = Equipment();
        var legacy = EquipmentState.Restore(Award(equipment).ToSnapshot() with
        { ActiveStyleId = "blueprint_fury", AdditiveVariantBonus = false });
        var frozen = EquipmentData.Create(legacy, equipment.Evaluator);
        var roundTrip = EquipmentData.Deserialize(frozen.Serialize());
        Assert.Equal(frozen.Serialize(), roundTrip.Serialize());
        Assert.False(roundTrip.State.AdditiveVariantBonus);
        Assert.False(legacy.Reinforce(equipment.Evaluator).AdditiveVariantBonus);
        Assert.True(legacy.ApplyVariant(equipment.Evaluator, "blueprint_arcane").AdditiveVariantBonus);
    }

    [Fact]
    public void Blueprint_guarantee_counts_only_new_completions_and_resets_after_a_reward()
    {
        var catalog = new EquipmentBlueprintCatalog { DropChance = 0.25, GuaranteeCompletions = 4 };
        var progress = new EquipmentBlueprintProgress();
        var firstRun = Guid.NewGuid();
        Assert.False(progress.Complete(firstRun, 0.99, catalog));
        Assert.False(progress.Complete(firstRun, 0, catalog));
        Assert.Equal(1, progress.Misses);
        Assert.False(progress.Complete(Guid.NewGuid(), 0.99, catalog));
        Assert.False(progress.Complete(Guid.NewGuid(), 0.99, catalog));
        Assert.True(progress.Complete(Guid.NewGuid(), 0.99, catalog));
        Assert.Equal(0, progress.Misses);
        Assert.False(progress.Complete(Guid.NewGuid(), 0.99, catalog));
        Assert.True(progress.Complete(Guid.NewGuid(), 0, catalog));
        Assert.Equal(0, progress.Misses);
    }

    [Fact]
    public void Conversion_quote_checks_ownership_payment_compatibility_and_fingerprints_blueprint_balance()
    {
        var equipment = Equipment();
        var catalog = Blueprints(equipment);
        var prices = JsonEquipmentUpgradePrices.Load(Path.Combine(Root, "equipment-upgrades.v1.json"));
        var state = Award(equipment);
        var instance = new EquipmentInstance { Id = state.Id, ItemBaseId = "shortsword" };
        instance.ApplyProgressionData(EquipmentData.Create(state, equipment.Evaluator));
        var stack = new InventoryItem { Quantity = 1, ItemInstance = new ItemInstance { ItemBaseId = "item.blueprint_fury" } };
        var context = new EquipmentUpgradeContext(new Character { Id = state.Ownership.OwnerId, Cinders = 1000 },
            new InventoryItem { Quantity = 1 }, instance, false, null, [], [stack]);
        var policy = new EquipmentUpgradePolicy(equipment, prices, catalog);
        var request = new EquipmentUpgradeRequest(EquipmentUpgradeOperationKind.ApplyVariant, state.Id, BlueprintStyleId: "blueprint_fury");
        var operation = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var quote = policy.Quote(context, request, operation, now);
        Assert.True(quote.CanExecute, quote.UnavailableReason);
        Assert.Equal(100, quote.CinderCost);
        Assert.Equal("Fury Shortsword", quote.After!.DisplayName);
        Assert.Equal(0, quote.PartsCost);
        Assert.Equal("set_fury", quote.After.EquipmentSetId);
        stack.Quantity = 0;
        var missing = policy.Quote(context, request, operation, now);
        Assert.False(missing.CanExecute);
        Assert.NotEqual(quote.Token, missing.Token);
        stack.Quantity = 1;
        context.Character.Cinders = 99;
        Assert.False(policy.Quote(context, request, operation, now).CanExecute);
        context.Character.Cinders = 1000;
        Assert.False(policy.Quote(context with { UnavailableReason = "Listed item" }, request, operation, now).CanExecute);
        Assert.False(policy.Quote(context, request with { BlueprintStyleId = "blueprint_warden" }, operation, now).CanExecute);
    }

    [Fact]
    public void Every_blueprint_source_has_a_choice_container_and_every_blueprint_has_a_source()
    {
        var equipment = Equipment();
        var catalog = Blueprints(equipment);
        foreach (var source in catalog.Sources)
        {
            var container = SelectionContainerCatalog.Find(source.SelectionItemId, catalog);
            Assert.NotNull(container);
            Assert.Equal(source.StyleIds.Order(), container.Options.Select(x => x.Id).Order());
            Assert.All(container.Options, x => Assert.Equal(1, x.Quantity));
        }
        Assert.All(equipment.Styles, style => Assert.NotNull(catalog.Find(style.Id)));
    }

    private static EquipmentState Award(StarterEquipmentCatalog equipment) => EquipmentState.Award(
        Guid.NewGuid(), equipment.Evaluator, "plain.shortsword", 1, 2,
        new(EquipmentAwardKind.RandomDiscovery, "test", "test"),
        new(EquipmentOwnershipKind.UnboundPersonal, Guid.NewGuid()), ItemQuality.Exceptional, 1.023);

    [Fact]
    public async Task Conversion_persists_stats_payment_and_receipt_and_retry_cannot_charge_twice()
    {
        var equipment = Equipment();
        var blueprints = Blueprints(equipment);
        var prices = JsonEquipmentUpgradePrices.Load(Path.Combine(Root, "equipment-upgrades.v1.json"));
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var state = Award(equipment);
        var character = new Character { Id = state.Ownership.OwnerId, Cinders = 1000 };
        var equipmentBase = new EquipmentBase { Id = "shortsword", Name = "Shortsword", EquipmentType = EquipmentType.OneHanded };
        var instance = new EquipmentInstance { Id = state.Id, ItemBaseId = equipmentBase.Id, ItemBase = equipmentBase };
        instance.ApplyProgressionData(EquipmentData.Create(state, equipment.Evaluator));
        var blueprintBase = new ItemBase { Id = "item.blueprint_fury", Name = "Blueprint: Fury", Stackable = true };
        var blueprintInstance = new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = blueprintBase.Id, ItemBase = blueprintBase };
        db.Characters.Add(character);
        db.InventoryItems.AddRange(
            new InventoryItem { InventoryId = character.Id, ItemInstanceId = instance.Id, ItemInstance = instance, Quantity = 1 },
            new InventoryItem { InventoryId = character.Id, ItemInstanceId = blueprintInstance.Id, ItemInstance = blueprintInstance, Quantity = 2 });
        await db.SaveChangesAsync();
        var repository = new EquipmentUpgradeRepository(db, blueprints);
        // Unequipped conversion does not settle combat or enqueue equipment-change events.
        var service = new EquipmentUpgradeService(equipment, prices, repository, null!, null!, TimeProvider.System, null!, blueprints);
        var request = new EquipmentUpgradeRequest(EquipmentUpgradeOperationKind.ApplyVariant, instance.Id, BlueprintStyleId: "blueprint_fury");
        var quote = await service.PreviewAsync(character.Id, request, default);
        Assert.True(quote.CanExecute, quote.UnavailableReason);
        var result = await service.ExecuteAsync(character.Id, quote.OperationId, request, quote.Token, default);
        Assert.NotNull(result.Outcome);
        await db.SaveChangesAsync();
        var retry = await service.ExecuteAsync(character.Id, quote.OperationId, request, quote.Token, default);
        Assert.Equal(result.Outcome, retry.Outcome);
        Assert.Equal(900, character.Cinders);
        Assert.Equal(1, (await db.InventoryItems.SingleAsync(x => x.ItemInstanceId == blueprintInstance.Id)).Quantity);
        Assert.Equal("blueprint_fury", instance.ProgressionData!.State.ActiveStyleId);
        Assert.Equal(state.Ownership, instance.ProgressionData.State.Ownership);
        Assert.Single(await db.EquipmentUpgradeReceipts.ToListAsync());
        Assert.Equal(2, await db.EconomyLedger.CountAsync());
        var reused = await service.ExecuteAsync(character.Id, quote.OperationId,
            request with { BlueprintStyleId = "blueprint_arcane" }, quote.Token, default);
        Assert.Null(reused.Outcome);
        Assert.Equal(900, character.Cinders);
    }
}
