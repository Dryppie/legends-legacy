using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerRuntimeComparisonTests
{
    private static CombatEntity Actor() => new(new Creature { Id = Guid.Empty, Name = "fixed" }) {
        Id = "actor", EquippedEssences = [new PlayerEssence { Id = Guid.Empty, EssenceDefinitionId = "essence", AbsorbedAt = DateTimeOffset.UnixEpoch, UpdatedAt = DateTimeOffset.UnixEpoch }],
        TemporaryModifiers = [new InstanceAttributeModifier(AttributeType.Power, 10) { Id = Guid.Empty },
            new ItemAttributeModifier(AttributeType.Power, 12) { Id = Guid.Empty }] };

    [Fact]
    public void Declared_essence_and_modifier_metadata_are_separated_and_their_values_remain_visible()
    {
        var a = Actor(); var b = Actor();
        b.EquippedEssences[0].AbsorbedAt = DateTimeOffset.UnixEpoch.AddDays(1);
        b.EquippedEssences[0].UpdatedAt = DateTimeOffset.UnixEpoch.AddDays(2);
        ((InstanceAttributeModifier)b.TemporaryModifiers[0]).Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        ((ItemAttributeModifier)b.TemporaryModifiers[1]).Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var left = TowerRuntimeComparison.Capture([a]); var right = TowerRuntimeComparison.Capture([b]);
        Assert.Equal(left.Hash, right.Hash); Assert.Empty(TowerRuntimeComparison.Differences(left.Values, right.Values));
        Assert.Equal(4, TowerRuntimeComparison.Differences(left.Bookkeeping, right.Bookkeeping).Count);
        Assert.Equal(8, right.Bookkeeping.Count); // Four values plus their explicit CLR types.
    }

    [Theory]
    [InlineData("actor-id")] [InlineData("essence-id")] [InlineData("definition")] [InlineData("level")]
    [InlineData("ascension")] [InlineData("evolution")] [InlineData("amount")] [InlineData("attribute")]
    [InlineData("counter")] [InlineData("power")] [InlineData("health")]
    public void Combat_and_source_identity_changes_are_not_hidden(string change)
    {
        var a = Actor(); var b = Actor();
        switch (change)
        {
            case "actor-id": b.Id = "changed"; break;
            case "essence-id": b.EquippedEssences[0].Id = Guid.Parse("00000000-0000-0000-0000-000000000001"); break;
            case "definition": b.EquippedEssences[0].EssenceDefinitionId = "changed"; break;
            case "level": b.EquippedEssences[0].Level++; break;
            case "ascension": b.EquippedEssences[0].AscensionTier++; break;
            case "evolution": b.EquippedEssences[0].IsEvolved = true; break;
            case "amount": b.TemporaryModifiers[0] = new InstanceAttributeModifier(AttributeType.Power, 11); break;
            case "attribute": b.TemporaryModifiers[0] = new InstanceAttributeModifier(AttributeType.MaxHealth, 10); break;
            case "counter": b.NextBasicAttackIn++; break;
            case "power": b.CombatAttributes[AttributeType.Power] = 42; break;
            case "health": b.CombatAttributes[AttributeType.MaxHealth] = 100; b.SetCurrentHealth(50); break;
        }
        var left = TowerRuntimeComparison.Capture([a]); var right = TowerRuntimeComparison.Capture([b]);
        Assert.NotEqual(left.Hash, right.Hash);
        Assert.NotEmpty(TowerRuntimeComparison.Differences(left.Values, right.Values));
    }

    [Fact]
    public void Missing_and_null_are_distinct_and_every_difference_is_retained()
    {
        var differences = TowerRuntimeComparison.Differences(new Dictionary<string,string?> { ["missing"] = null, ["value"] = "before" },
            new Dictionary<string,string?> { ["value"] = "after", ["added"] = null });
        Assert.Equal(3, differences.Count); Assert.Contains(differences, d => d.Path == "missing" && d.LeftPresent && !d.RightPresent);
    }

    [Fact]
    public void Rehydrated_equipment_acquisition_time_is_metadata_but_equipment_identity_is_not()
    {
        var a = Actor(); var b = Actor();
        a.Equipment = [new EquipmentInstance { Id = Guid.Empty, AcquiredAtUtc = DateTimeOffset.UnixEpoch }];
        b.Equipment = [new EquipmentInstance { Id = Guid.Empty, AcquiredAtUtc = DateTimeOffset.UnixEpoch.AddDays(1) }];
        var left = TowerRuntimeComparison.Capture([a]); var right = TowerRuntimeComparison.Capture([b]);
        Assert.Equal(left.Hash, right.Hash);
        var difference = Assert.Single(TowerRuntimeComparison.Differences(left.Bookkeeping, right.Bookkeeping));
        Assert.EndsWith("Domain.Models.Items.ItemInstance::<AcquiredAtUtc>k__BackingField", difference.Path);
        b.Equipment[0].Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Assert.NotEqual(left.Hash, TowerRuntimeComparison.Capture([b]).Hash);
    }

    [Fact]
    public void Comparison_is_read_only_and_cancellation_is_honored()
    {
        var a = Actor(); var before = TowerRuntimeComparison.Capture([a]);
        Assert.Equal(before.Hash, TowerRuntimeComparison.Capture([a]).Hash);
        Assert.Equal(DateTimeOffset.UnixEpoch, a.EquippedEssences[0].AbsorbedAt);
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.Throws<OperationCanceledException>(() => TowerRuntimeComparison.Capture([a], stop.Token));
    }
}
