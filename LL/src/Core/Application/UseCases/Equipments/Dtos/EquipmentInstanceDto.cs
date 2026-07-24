using Application.Common.Mappings;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Tools;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Equipments.Dtos;
public class EquipmentInstanceDto : ItemInstanceDto, IMapFrom<EquipmentInstance>
{
    public string DisplayName { get; set; } = string.Empty;
    public Rarity Rarity { get; set; } = Rarity.Common;
    public ItemQuality Quality { get; set; } = ItemQuality.Standard;
    public string? BaseRecipeId { get; set; }
    public string? BlueprintId { get; set; }
    public EquipmentCraftingDesignMetadataDto? CraftingDesign { get; set; }
    public string? CraftedName { get; set; }
    public int Tier { get; set; } = 1;
    public int? Potential { get; set; } = null;
    public int? MaxPotential { get; set; } = null;
    public int TemperingProgress { get; set; } = 0;
    public EquipmentBase EquipmentBase { get; set; } = null!;
    public int ItemXp { get; set; } = 0;
    public IReadOnlyCollection<ItemAttributeModifier> BaseModifiers { get; set; } = [];
    public List<InstanceAttributeModifier> InstanceModifiers { get; set; } = [];
    public List<AttributeModifierBase> AttributeModifiers { get; set; } = [];
    public List<ToolBonusModifier> ToolAffixes { get; set; } = [];
    public IReadOnlyList<ToolBonusModifier> EffectiveToolBonuses { get; set; } = [];
    public List<string> AffinityTags { get; set; } = [];
    public double ItemBudget { get; set; }
    public int ItemBudgetTier { get; set; }
    public int BalanceVersion { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquipmentInstance, EquipmentInstanceDto>()
            .ForMember(
                destination => destination.ItemBudget,
                options => options.MapFrom(source =>
                    EquipmentBudgetEvaluator.Evaluate(source.AttributeModifiers, source.Tier)))
            .ForMember(
                destination => destination.ItemBudgetTier,
                options => options.MapFrom(source => source.Tier))
            .ForMember(
                destination => destination.BalanceVersion,
                options => options.MapFrom(_ => EquipmentBudgetEvaluator.BalanceVersion))
            .ForMember(
                destination => destination.CraftingDesign,
                options => options.MapFrom<EquipmentCraftingDesignMetadataResolver>());
    }
}
