using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Items.Dtos;
public class EquipmentBaseDto : ItemBaseDto, IMapFrom<EquipmentBase>
{
    public EquipmentType EquipmentType { get; set; }
    public ICollection<ItemAttributeModifier> AttributeModifiers { get; set; } = [];
    public double ItemBudget { get; set; }
    public int ItemBudgetTier { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EquipmentBase, EquipmentBaseDto>()
            .ForMember(
                destination => destination.ItemBudget,
                options => options.MapFrom(source => EquipmentBudgetEvaluator.Evaluate(
                    source.AttributeModifiers,
                    EquipmentStatBudgetCatalog.MinimumTier)))
            .ForMember(
                destination => destination.ItemBudgetTier,
                options => options.MapFrom(_ => EquipmentStatBudgetCatalog.MinimumTier));
    }
}
