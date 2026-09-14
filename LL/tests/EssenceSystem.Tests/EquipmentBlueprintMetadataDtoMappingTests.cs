using Application;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.DependencyInjection;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class EquipmentBlueprintMetadataDtoMappingTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IMapper _mapper;

    public EquipmentBlueprintMetadataDtoMappingTests()
    {
        var equipmentRoot = Path.Combine(
            TestContentPaths.FindApiRoot(), "Data", "equipment");
        var equipment = JsonStarterEquipmentCatalog.Load(
            Path.Combine(equipmentRoot, "equipment-starters.v1.json"));
        var blueprints = JsonEquipmentBlueprintCatalog.Load(
            Path.Combine(equipmentRoot, "equipment-blueprints.v1.json"),
            equipment);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<EquipmentCatalog>(equipment);
        services.AddSingleton(blueprints);
        services.AddApplication();
        _provider = services.BuildServiceProvider();
        _mapper = _provider.GetRequiredService<IMapper>();
    }

    [Fact]
    public void Blueprint_items_include_their_attributes_and_set_bonuses()
    {
        var item = _mapper.Map<ItemBaseDto>(new ItemBase
        {
            Id = "item.blueprint_arcane",
            Name = "Blueprint: Arcane"
        });

        Assert.NotNull(item.Blueprint);
        Assert.Equal("blueprint_arcane", item.Blueprint.StyleId);
        Assert.Equal(
            [
                AttributeType.Power,
                AttributeType.MagicPenetration,
                AttributeType.Cooldown,
                AttributeType.CritChance
            ],
            item.Blueprint.Attributes);
        Assert.Equal("set_arcane", item.Blueprint.EquipmentSet?.Id);
        Assert.Collection(
            item.Blueprint.EquipmentSet!.Bonuses,
            bonus =>
            {
                Assert.Equal(2, bonus.RequiredEquippedItems);
                Assert.Contains("Magic Penetration", bonus.Description);
            },
            bonus =>
            {
                Assert.Equal(4, bonus.RequiredEquippedItems);
                Assert.Contains("Every third active ability", bonus.Description);
            });
    }

    [Fact]
    public void Non_blueprint_items_do_not_include_blueprint_metadata()
    {
        var item = _mapper.Map<ItemBaseDto>(new ItemBase
        {
            Id = "item.reinforcement_parts",
            Name = "Reinforcement Parts"
        });

        Assert.Null(item.Blueprint);
    }

    public void Dispose() => _provider.Dispose();
}
