using Application.Common.Mappings;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Models.Items;

namespace Application.UseCases.Items.Dtos;
public class ItemDto : IMapFrom<Item>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public Rarity Rarity { get; set; }
    public EssenceDetailsDto? Essence { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Item, ItemDto>()
            .ForMember(dest => dest.Essence, opt => opt.MapFrom<EssenceResolver>());
    }

    public class EssenceResolver : IValueResolver<Item, ItemDto, EssenceDetailsDto?>
    {
        public EssenceDetailsDto? Resolve(Item source, ItemDto destination, EssenceDetailsDto? destMember, ResolutionContext context)
        {
            if (source is EssenceItem essenceItem && essenceItem.Essence != null)
            {
                // Map the Essence entity to the EssenceDetailsDto using AutoMapper
                return context.Mapper.Map<EssenceDetailsDto>(essenceItem.Essence);
            }
            return null;
        }
    }
}