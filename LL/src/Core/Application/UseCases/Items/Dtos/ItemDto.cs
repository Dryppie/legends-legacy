using Application.Common.Mappings;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Models.Essences;
using Domain.Models.Items;

namespace Application.UseCases.Items.Dtos;
public class ItemDto : IMapFrom<Item>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public Rarity Rarity { get; set; }
    public EssenceDto? Essence { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Item, ItemDto>()
            .ForMember(dest => dest.Essence, opt => opt.MapFrom<EssenceResolver>())
            .ForMember(dest => dest.Description, opt => opt.MapFrom<DescriptionResolver>());
    }

    public class EssenceResolver : IValueResolver<Item, ItemDto, EssenceDto?>
    {
        public EssenceDto? Resolve(Item source, ItemDto destination, EssenceDto? destMember, ResolutionContext context)
        {
            if (source is EssenceItem essenceItem && essenceItem.Essence != null)
            {
                // Map the Essence entity to the EssenceDto using AutoMapper
                return context.Mapper.Map<EssenceDto>(essenceItem.Essence);
            }
            return null;
        }
    }

    public class DescriptionResolver : IValueResolver<Item, ItemDto, string>
    {
        public string Resolve(Item source, ItemDto destination, string destMember, ResolutionContext context)
        {
            if (source is EssenceItem essenceItem && essenceItem.Essence != null)
            {
                // Build description from ActiveAbility and PassiveAbility
                var activeAbility = essenceItem.Essence.ActiveAbility.Description ?? "No Active Ability";
                var passiveAbility = essenceItem.Essence.PassiveAbility.Description ?? "No Passive Ability";

                return $"Active: {activeAbility}\n\nPassive: {passiveAbility}";
            }

            // Default description for non-Essence items
            return source.Description ?? string.Empty;
        }
    }
}