using Application.UseCases.Inventories.SelectionCrates;
using Application.UseCases.Items.Dtos;
using Domain.Models.Items;
using Services.LL.Inventories;

namespace EssenceSystem.Tests;

public sealed partial class SelectionCrateServiceTests
{
    [Theory]
    [InlineData("item.essence_token.lumo_ruins", "goblin", "item.essence.goblin")]
    public async Task EquipmentProgression_preserves_supported_container_choices(string containerId, string option, string rewardId)
    {
        var owner = Guid.NewGuid();
        var crate = CreateInventoryItem(owner, containerId, ItemType.Resource, 1);
        var service = new SelectionCrateService(new FakeInventoryService(crate),
            new FakeItemBaseRepository([new ItemBase { ItemType = ItemType.Resource, Id = rewardId, Name = rewardId, Stackable = true }]),
            new InventoryItemFactory());
        var result = await service.OpenSelectionContainerAsync(owner, crate.ItemInstanceId, option, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(rewardId, Assert.Single(result.Rewards).ItemInstance.ItemBaseId);
    }

}
