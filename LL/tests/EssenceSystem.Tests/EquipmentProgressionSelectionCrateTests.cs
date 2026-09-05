using Application.Interfaces.Services.LL.Items;
using Application.UseCases.Inventories.SelectionCrates;
using Application.UseCases.Items.Dtos;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Inventories;
using Services.LL.Items;

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

    [Fact]
    public async Task Arms_chest_consumes_once_and_awards_the_selected_authored_starter_weapon()
    {
        var owner = Guid.NewGuid();
        var chest = CreateInventoryItem(owner, TutorialArmsChestCatalog.ItemBaseId, ItemType.Resource, 1);
        var starterEquipment = new RecordingStarterEquipmentService(owner);
        var service = new SelectionCrateService(
            new FakeInventoryService(chest),
            new FakeItemBaseRepository([]),
            new InventoryItemFactory(),
            starterEquipment);

        var result = await service.OpenSelectionContainerAsync(
            owner,
            chest.ItemInstanceId,
            "plain.wand",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.RewardsAlreadyPublished);
        Assert.Equal("Arms Chest", result.ContainerName);
        Assert.Equal(0, chest.Quantity);
        Assert.Equal("plain.wand", starterEquipment.SelectedDefinitionId);
        var weapon = Assert.IsType<EquipmentInstance>(Assert.Single(result.Rewards).ItemInstance);
        Assert.True(weapon.IsBound);
        Assert.Equal(1, weapon.ProgressionData!.State.Tier);
        Assert.Equal(0, weapon.ProgressionData.State.Rank);
        Assert.Equal("plain.wand", weapon.ProgressionData.State.DefinitionId);
    }

    private sealed class RecordingStarterEquipmentService(Guid owner) : IStarterEquipmentService
    {
        public string? SelectedDefinitionId { get; private set; }

        public Task<StarterEquipmentClaimResult> ClaimAsync(
            Guid characterId,
            StarterEquipmentGrantKind kind,
            IReadOnlyList<string> definitionIds,
            CancellationToken cancellationToken)
        {
            Assert.Equal(owner, characterId);
            Assert.Equal(StarterEquipmentGrantKind.FirstWeapon, kind);
            SelectedDefinitionId = Assert.Single(definitionIds);
            var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(
                TestContentPaths.FindApiRoot(),
                "Data/equipment/equipment-starters.v1.json"));
            var data = EquipmentData.Create(
                EquipmentState.Award(
                    Guid.NewGuid(),
                    catalog.Evaluator,
                    SelectedDefinitionId,
                    1,
                    0,
                    new(EquipmentAwardKind.QuestReward, "quest.onboarding.first_weapon", "test"),
                    new(EquipmentOwnershipKind.BoundPersonal, owner)),
                catalog.Evaluator);
            var instance = new EquipmentInstance
            {
                Id = data.State.Id,
                ItemBaseId = data.ItemBaseId,
                ItemBase = new EquipmentBase
                {
                    Id = data.ItemBaseId,
                    Name = data.DisplayName,
                    EquipmentType = data.EquipmentType
                }
            };
            instance.ApplyProgressionData(data);
            var reward = new InventoryItem
            {
                InventoryId = owner,
                ItemInstanceId = instance.Id,
                ItemInstance = instance,
                Quantity = 1
            };
            var grant = new StarterEquipmentGrant(owner, kind, [data], DateTimeOffset.UtcNow);
            return Task.FromResult(new StarterEquipmentClaimResult(grant, null) { Rewards = [reward] });
        }

        public Task<EquipmentAccess> GetAccessAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public IReadOnlyList<StarterEquipmentOption> GetOptions() => throw new NotSupportedException();
    }

}
