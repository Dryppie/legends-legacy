using System.Text.Json.Serialization;
using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;

namespace Application.UseCases.Items.Dtos;
[JsonPolymorphic(TypeDiscriminatorPropertyName = "ItemType")]
[JsonDerivedType(typeof(EssenceItemBaseDto), "Essence")]
[JsonDerivedType(typeof(EquipmentBaseDto), "Equipment")]
public class ItemBaseDto : IMapFrom<ItemBase>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public Rarity Rarity { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<ItemBase, ItemBaseDto>()
            .Include<EssenceItemBase, EssenceItemBaseDto>()
            .Include<EquipmentBase, EquipmentBaseDto>()
            /*.ForMember(dest => dest.Essence, opt => opt.MapFrom<EssenceResolver>())*/;
    }

    //public class EssenceResolver : IValueResolver<ItemBase, ItemBaseDto, EssenceDetailsDto?>
    //{
    //    public EssenceDetailsDto? Resolve(ItemBase source, ItemBaseDto destination, EssenceDetailsDto? destMember, ResolutionContext context)
    //    {
    //        if (source is EssenceItemBase essenceItem && essenceItem.Essence != null)
    //        {
    //            // Map the Essence entity to the EssenceDetailsDto using AutoMapper
    //            return context.Mapper.Map<EssenceDetailsDto>(essenceItem.Essence);
    //        }
    //        return null;
    //    }
    //}
}