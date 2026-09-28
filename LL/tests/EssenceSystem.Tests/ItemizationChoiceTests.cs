using Microsoft.EntityFrameworkCore;
using Domain.Models.Analytics;
using Domain.Models.Attributes;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class ItemizationChoiceTests
{
    private static readonly EquipmentSlotType[] Slots = Enum.GetValues<EquipmentSlotType>();
    private static EquipmentInstance Gear(Guid owner, string definition = "plain.heavy_helm", int tier = 1,
        EquipmentOwnershipKind kind = EquipmentOwnershipKind.UnboundPersonal)
    {
        var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(TestContentPaths.FindApiRoot(), "Data", "equipment", "equipment-starters.v1.json"));
        var state = EquipmentState.Award(Guid.NewGuid(), catalog.Evaluator, definition, tier, 0,
            new(EquipmentAwardKind.RandomDiscovery, "combat", "award"), new(kind, owner));
        var data = EquipmentData.Create(state, catalog.Evaluator);
        var item = new EquipmentInstance { Id = state.Id, ItemBaseId = data.ItemBaseId };
        item.ApplyProgressionData(data);
        return item;
    }

    [Fact]
    public async Task Repository_excludes_empty_and_absent_inventory_and_requires_current_guild_membership()
    {
        await using var db = new Persistence.LL.LLDbContext(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Persistence.LL.LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var owner = Guid.NewGuid(); var guild = Guid.NewGuid();
        var candidate = Gear(owner); var empty = Gear(owner); var absent = Gear(owner);
        var loan = Gear(guild, kind: EquipmentOwnershipKind.GuildOwned);
        var character = new Domain.Models.Entities.Characters.Character { Id = owner, Level = 30 };
        character.Inventory = new() { CharacterId = owner, Character = character, InventoryItems =
            new[] { candidate, empty, loan }.Select(x => new Domain.Models.Inventories.InventoryItem
                { InventoryId = owner, ItemInstanceId = x.Id, ItemInstance = x, Quantity = x == empty ? 0 : 1 }).ToList() };
        character.EquipmentSlots = Slots.Select(x => new EquipmentSlot { EntityId = owner, EquipmentSlotType = x }).ToList();
        db.Characters.Add(character); db.ItemInstances.Add(absent);
        db.GuildVaultItems.Add(new() { GuildId = guild, EquipmentInstanceId = loan.Id, BorrowedByCharacterId = owner });
        await db.SaveChangesAsync();
        var repository = new Persistence.LL.Repositories.Analytics.ItemizationChoiceRepository(db);
        var inventory = (await repository.CaptureAsync(owner, default))!;
        Assert.DoesNotContain(inventory.Items, x => x.ItemId == empty.Id || x.ItemId == absent.Id);
        Assert.Equal("invalid-guild-loan", inventory.Items.Single(x => x.ItemId == loan.Id).Ineligibility);
        db.GuildMembers.Add(new() { GuildId = guild, CharacterId = owner }); await db.SaveChangesAsync();
        Assert.True((await repository.CaptureAsync(owner, default))!.Items.Single(x => x.ItemId == loan.Id).Eligible);
    }

    [Fact]
    public void Eligibility_observes_level_owner_slot_and_guild_loan_not_just_inventory_presence()
    {
        var owner = Guid.NewGuid(); var guild = Guid.NewGuid();
        var candidate = Gear(owner); var high = Gear(owner, tier: 2); var foreign = Gear(Guid.NewGuid());
        var loan = Gear(guild, kind: EquipmentOwnershipKind.GuildOwned);
        var unavailableLoan = Gear(guild, kind: EquipmentOwnershipKind.GuildOwned);
        var inventory = ItemizationChoiceInventory.Capture(owner, 1, Slots,
            new[] { candidate, high, foreign, loan, unavailableLoan }.Select(x => (x, false)), new HashSet<Guid> { loan.Id });
        var choice = inventory.ForItem(candidate.Id)!;
        Assert.True(choice.Complete); Assert.True(choice.CandidateEligible);
        Assert.Equal(1, choice.EligibleAlternatives);
        Assert.Equal("level", choice.Alternatives.Single(x => x.ItemId == high.Id).Ineligibility);
        Assert.Equal("different-owner", choice.Alternatives.Single(x => x.ItemId == foreign.Id).Ineligibility);
        Assert.Equal("invalid-guild-loan", choice.Alternatives.Single(x => x.ItemId == unavailableLoan.Id).Ineligibility);
        var missingSlot = ItemizationChoiceInventory.Capture(owner, 1, [], [(candidate, false)], new HashSet<Guid>());
        Assert.Equal("missing-slot", missingSlot.ForItem(candidate.Id)!.CandidateIneligibility);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EquipmentSlotType.Head)]
    public async Task Equip_observation_uses_the_committed_hand_slot_when_the_request_uses_a_fallback(EquipmentSlotType? requested)
    {
        await using var db = new Persistence.LL.LLDbContext(new DbContextOptionsBuilder<Persistence.LL.LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var owner = Guid.NewGuid(); var candidate = Gear(owner, "plain.dagger"); var shield = Gear(owner, "plain.towershield");
        var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(TestContentPaths.FindApiRoot(), "Data", "equipment", "equipment-starters.v1.json"));
        candidate.ItemBase = catalog.GetEquipmentBase(candidate.ItemBaseId);
        shield.ItemBase = catalog.GetEquipmentBase(shield.ItemBaseId);
        var character = new Domain.Models.Entities.Characters.Character { Id = owner, Level = 30 };
        character.Inventory = new() { CharacterId = owner, Character = character, InventoryItems =
            new[] { candidate, shield }.Select(x => new Domain.Models.Inventories.InventoryItem
                { InventoryId = owner, ItemInstanceId = x.Id, ItemInstance = x, Quantity = 1 }).ToList() };
        character.EquipmentSlots = Slots.Select(x => new EquipmentSlot { EntityId = owner, EquipmentSlotType = x }).ToList();
        db.Characters.Add(character); await db.SaveChangesAsync();
        var outbox = new RecordingItemizationOutbox();
        var service = new EquipmentSlotService(new Persistence.LL.Repositories.Equipments.EquipmentSlotRepository(db),
            new Persistence.LL.Repositories.Inventories.InventoryRepository(db), outbox: outbox,
            choices: new Persistence.LL.Repositories.Analytics.ItemizationChoiceRepository(db));

        Assert.True((await service.EquipEquipmentAsync(owner, candidate.Id, requested, default)).Succeeded);
        var observation = Assert.IsType<ItemizationObservation>(Assert.Single(outbox.Events).Payload);
        Assert.True(observation.Choices!.CandidateEligible);
        Assert.Empty(observation.Choices.Alternatives); // The shield cannot replace the committed main-hand choice.
        Assert.Equal(candidate.Id, character.EquipmentSlots.Single(x => x.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstanceId);
    }

    [Fact]
    public void Two_hand_items_are_counted_once_and_other_slots_do_not_inflate_available_choices()
    {
        var owner = Guid.NewGuid(); var staff = Gear(owner, "plain.staff"); var dagger = Gear(owner, "plain.dagger"); var helm = Gear(owner);
        var inventory = ItemizationChoiceInventory.Capture(owner, 30, Slots,
            [(staff, true), (staff, true), (dagger, false), (helm, false)], new HashSet<Guid>());
        var choice = inventory.ForItem(dagger.Id, EquipmentSlotType.MainHand)!;
        Assert.Equal(staff.Id, Assert.Single(choice.Alternatives).ItemId);
        Assert.True(choice.Alternatives[0].Equipped);
        Assert.False(inventory.ForItem(dagger.Id, EquipmentSlotType.Head)!.CandidateEligible);
        Assert.Empty(inventory.ForItem(helm.Id)!.Alternatives);
    }

    [Fact]
    public void Same_batch_awards_are_available_but_unversioned_gear_marks_snapshot_incomplete()
    {
        var owner = Guid.NewGuid(); var first = Gear(owner); var second = Gear(owner);
        var inventory = ItemizationChoiceInventory.Capture(owner, 30, Slots, [], new HashSet<Guid>())
            .WithAwards([first.ProgressionData!, second.ProgressionData!]);
        Assert.Equal(second.Id, Assert.Single(inventory.ForItem(first.Id)!.Alternatives).ItemId);
        var legacy = new EquipmentInstance { Id = Guid.NewGuid() };
        var partial = ItemizationChoiceInventory.Capture(owner, 30, Slots, [(first, false), (legacy, true)], new HashSet<Guid>());
        Assert.False(partial.ForItem(first.Id)!.Complete);
        Assert.Equal(1, partial.ForItem(first.Id)!.UnversionedItems);
    }

    [Fact]
    public void Eligible_denominators_exclude_unknown_and_incomplete_history_but_include_known_empty_choice_sets()
    {
        var owner = Guid.NewGuid(); var item = Gear(owner); var data = item.ProgressionData!;
        var choice = ItemizationChoiceInventory.Capture(owner, 30, Slots, [(item, false)], new HashSet<Guid>()).ForItem(item.Id)!;
        var start = new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero);
        ItemizationObservation Award(string id, ItemizationChoiceContext? context) => new(id, "awarded", owner,
            start, "combat", 18, item.Id, data) { Choices = context };
        var report = Assert.Single(ItemizationOpportunity.Create(new(2026, 9, 27),
            [Award("known", choice), Award("old", null), Award("partial", choice with { Complete = false }),
                new("equip", "equipped", owner, start.AddHours(1), "manual", 18, item.Id, data)]));
        Assert.Equal(3, report.Awarded); Assert.Equal(1, report.AwardsWithCompleteChoices);
        Assert.Equal(1, report.EligibleAwards); Assert.Equal(1, report.EligibleAwardsEquipped);
        Assert.Equal(0, report.EligibleAwardsWithAlternatives);
        var decision = Award("decision", choice) with { Kind = "equipped" };
        var summary = Assert.Single(ItemizationChoiceSummary.Create([decision, decision, decision with { Id = "old", Choices = null }]));
        Assert.Equal(2, summary.Decisions); Assert.Equal(1, summary.MissingOrIncomplete);
        Assert.Equal(1, summary.EligibleDecisions); Assert.Equal(0, summary.DecisionsWithAlternatives);
        Assert.All(summary.Attributes, x => Assert.Equal(1, x.SelectedDecisions));
    }
}
