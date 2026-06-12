using Application.Common.Mappings;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;

namespace Application.UseCases.Characters.Dtos;
public class CharacterOverviewDto : IMapFrom<Character>
{
    public Guid Id { get; set; }
    public int Level { get; set; }
    public List<EntityAttribute> BaseAttributes { get; set; } = [];
    public List<EntityAttribute> BaseCombatAttributes { get; set; } = [];
    public EssenceLoadoutDto? ActiveEssenceLoadout { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Character, CharacterOverviewDto>()
            .ForMember(dest => dest.BaseCombatAttributes, opt => opt.MapFrom(src =>
                src.BaseCombatAttributes.Select(kvp => new EntityAttribute
                {
                    EntityId = src.Id,
                    AttributeType = kvp.Key,
                    Value = kvp.Value
                }).ToList()
            ))
            .ForMember(dest => dest.ActiveEssenceLoadout, opt => opt.MapFrom(src =>
                src.EssenceLoadouts
                    .Where(loadout => loadout.IsActive)
                    .Select(loadout => new EssenceLoadoutDto(
                        loadout.Id,
                        loadout.Name,
                        loadout.IsActive,
                        loadout.Slots
                            .OrderBy(slot => slot.SlotIndex)
                            .Select(slot => new EssenceLoadoutSlotDto(
                                slot.SlotIndex,
                                slot.PlayerEssenceId,
                                slot.PlayerEssence == null ? null : slot.PlayerEssence.EssenceDefinitionId,
                                slot.PlayerEssence == null ? null : slot.PlayerEssence.EssenceDefinitionId))
                            .ToList()))
                    .FirstOrDefault()
            ));
    }
}
