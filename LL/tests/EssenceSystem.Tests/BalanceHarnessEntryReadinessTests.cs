using BalanceHarness;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessEntryReadinessTests
{
    private static readonly Lazy<IReadOnlyList<FixtureEquipment>> Gear = new(() =>
    {
        var root = TestContentPaths.FindApiRoot();
        var content = OfflineContent.ForTower(root, TowerBundle.ReadSettings(root));
        var evaluator = content.Equipment.Evaluator;
        return new[] { EquipmentType.Head, EquipmentType.Chest, EquipmentType.Legs, EquipmentType.Necklace,
            EquipmentType.Ring, EquipmentType.Relic, EquipmentType.TwoHanded, EquipmentType.OneHanded, EquipmentType.OffHand }
            .Select(type =>
            {
                var definition = evaluator.Definitions.First(d => d.Rarity == EquipmentRarity.Common
                    && d.SpecializationId == "default" && evaluator.GetArchetype(d.ArchetypeId).EquipmentType == type);
                var data = EquipmentData.Create(EquipmentState.Award(Guid.NewGuid(), evaluator, definition.Id, 1, 0,
                    new(EquipmentAwardKind.RandomDiscovery, "test", "test"), new(EquipmentOwnershipKind.BoundPersonal, Guid.NewGuid())), evaluator);
                var slot = type is EquipmentType.TwoHanded or EquipmentType.OneHanded ? EquipmentSlotType.MainHand : Enum.Parse<EquipmentSlotType>(type.ToString());
                return new FixtureEquipment(slot, data);
            }).ToArray();
    });
    private static IReadOnlyList<FixtureEquipment> TwoHanded => Gear.Value.Where(e => e.Data.EquipmentType is not (EquipmentType.OneHanded or EquipmentType.OffHand)).ToArray();
    private static Dictionary<string, int> Stock => new() { ["sigil_goblin_mines"] = 1, ["sigil_forgotten_catacombs"] = 2 };

    [Theory]
    [InlineData(EquipmentSlotType.Head)]
    [InlineData(EquipmentSlotType.Chest)]
    [InlineData(EquipmentSlotType.Legs)]
    [InlineData(EquipmentSlotType.Necklace)]
    [InlineData(EquipmentSlotType.Ring)]
    [InlineData(EquipmentSlotType.Relic)]
    [InlineData(EquipmentSlotType.MainHand)]
    public void Missing_equipped_slot_waits_without_spending_stock(EquipmentSlotType absent)
    {
        var stock = Stock;
        var decision = TowerEntryReadiness.Decide("full-slot-ready", TwoHanded.Where(e => e.Slot != absent).ToArray(), stock, 2160, true, 0, 0);
        Assert.Equal("EquipmentCoverage", decision.Reason);
        Assert.Null(decision.Dungeon);
        Assert.Contains(absent, decision.MissingSlots);
        Assert.Equal(1, stock["sigil_goblin_mines"]);
        Assert.Equal(2, stock["sigil_forgotten_catacombs"]);
    }

    [Fact]
    public void Common_rank_zero_coverage_accepts_two_hands_or_one_hand_with_offhand()
    {
        Assert.All(Gear.Value, e => { Assert.Equal(EquipmentRarity.Common, e.Data.Rarity); Assert.Equal(0, e.Data.State.Rank); });
        Assert.Empty(TowerEntryReadiness.MissingSlots(TwoHanded));
        var oneHanded = Gear.Value.Where(e => e.Data.EquipmentType != EquipmentType.TwoHanded).ToArray();
        Assert.Empty(TowerEntryReadiness.MissingSlots(oneHanded));
        Assert.Equal(new[] { EquipmentSlotType.OffHand }, TowerEntryReadiness.MissingSlots(oneHanded.Where(e => e.Slot != EquipmentSlotType.OffHand).ToArray()));
        Assert.Equal("Enter", TowerEntryReadiness.Decide("full-slot-ready", TwoHanded, Stock, 8640, true, 0, 0).Reason);
    }

    [Theory]
    [InlineData(false, 0, 0, "QuestGate")]
    [InlineData(true, 12, 3, "AttemptCap")]
    [InlineData(true, 7, 7, "Complete")]
    public void Readiness_does_not_bypass_quest_or_stopping_rules(bool quest, int attempts, int earned, string reason)
    {
        Assert.Equal(reason, TowerEntryReadiness.Decide("full-slot-ready", TwoHanded, Stock, 25920, quest, attempts, earned).Reason);
    }

    [Fact]
    public void Immediate_arm_ignores_coverage_but_both_arms_require_real_source_stock()
    {
        Assert.Equal("goblin_mines", TowerEntryReadiness.Decide("mines-first-either", [], Stock, 2160, true, 0, 0).Dungeon);
        var stock = Stock; stock["sigil_goblin_mines"] = 0;
        Assert.Equal("forgotten_catacombs", TowerEntryReadiness.Decide("full-slot-ready", TwoHanded, stock, 8640, true, 0, 0).Dungeon);
        stock["sigil_forgotten_catacombs"] = 0;
        Assert.Equal("NoSigil", TowerEntryReadiness.Decide("full-slot-ready", TwoHanded, stock, 8640, true, 0, 0).Reason);
    }

    [Fact]
    public void Declaration_reuses_activity_identity_and_rejects_hidden_outcome_policies()
    {
        var fixtures = Path.GetFullPath(Path.Combine(TestContentPaths.FindApiRoot(), "../../../tools/BalanceHarness/Fixtures"));
        var plan = TowerEntryReadiness.Read(fixtures);
        Assert.Equal(TowerActivityInventory.Version, plan.ActivityVersion);
        Assert.Equal(new[] { "mines-first-either", "full-slot-ready" }, plan.Policies);
        Assert.Throws<InvalidDataException>(() => TowerEntryReadiness.Decide("peek-at-boss", [], Stock, 2160, true, 0, 0));
    }

    private sealed class StudyFactAttribute : FactAttribute
    {
        public StudyFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_ENTRY_READINESS")))
                Skip = "Requires frozen entry-readiness owner; ordinary tests execute zero fights.";
        }
    }
    [StudyFact]
    public async Task Frozen_paired_entry_readiness()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(14));
        await TowerActivityStudy.RunEntryReadinessAsync(HarnessJson.Read<TowerActivityRequest>(
            Environment.GetEnvironmentVariable("LL_TOWER_ENTRY_READINESS")!), deadline.Token);
    }
}
