using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Tools;
using Domain.Models.Professions.Gathering.GatheringNodes;
using Services.LL.Inventories;
using Services.LL.Interfaces.Combat.Reward;

namespace EssenceSystem.Tests;

public sealed class InventoryItemFactoryToolTests
{
    [Fact]
    public void Rare_material_affix_uses_the_catalytic_profile_name()
    {
        var random = new ProfileSelectingRandomSource(2, 3, 0, 4, 5);
        var factory = new InventoryItemFactory(random);
        var toolBase = new EquipmentBase
        {
            Id = "test_pickaxe",
            Name = "Test Pickaxe",
            ItemType = ItemType.Equipment,
            EquipmentType = EquipmentType.Tool,
            Rarity = Rarity.Uncommon,
            GatheringType = GatheringType.Mining
        };

        var inventoryItem = factory.Create(toolBase, 1);

        var tool = Assert.IsType<EquipmentInstance>(inventoryItem.ItemInstance);
        var affix = Assert.Single(tool.ToolAffixes);
        Assert.Equal(ToolBonusType.RareMaterialChancePercent, affix.BonusType);
        Assert.Equal("Catalytic", affix.Name);
    }

    private sealed class ProfileSelectingRandomSource(params int[] sortValues) : IResolutionRandomSource
    {
        private readonly Queue<int> _sortValues = new(sortValues);

        public IDisposable UseSeed(int seed) => NoopDisposable.Instance;
        public Guid NextGuid() => Guid.NewGuid();
        public int NextInt(int exclusiveMaximum) => _sortValues.Dequeue();
        public double NextDouble() => 0d;

        private sealed class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance { get; } = new();
            public void Dispose()
            {
            }
        }
    }
}
