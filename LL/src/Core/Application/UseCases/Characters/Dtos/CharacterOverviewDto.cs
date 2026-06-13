using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;

namespace Application.UseCases.Characters.Dtos;
public class CharacterOverviewDto : IMapFrom<Character>
{
    public Guid Id { get; set; }
    public int Level { get; set; }
    public int PowerScore { get; set; }
    public List<EntityAttribute> BaseAttributes { get; set; } = [];
    public List<EntityAttribute> BaseCombatAttributes { get; set; } = [];
    public EssenceLoadoutDto? ActiveEssenceLoadout { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Character, CharacterOverviewDto>()
            .ConvertUsing<CharacterOverviewConverter>();
    }
}

public sealed class CharacterOverviewConverter : ITypeConverter<Character, CharacterOverviewDto>
{
    private readonly IEssenceDefinitionRepository _essenceDefinitions;

    public CharacterOverviewConverter(IEssenceDefinitionRepository essenceDefinitions)
    {
        _essenceDefinitions = essenceDefinitions;
    }

    public CharacterOverviewDto Convert(Character source, CharacterOverviewDto destination, ResolutionContext context)
    {
        return new CharacterOverviewDto
        {
            Id = source.Id,
            Level = source.Level,
            PowerScore = PowerScoreCalculator.Calculate(source.BaseCombatAttributes, source.Level),
            BaseAttributes = source.BaseAttributes.ToList(),
            BaseCombatAttributes = source.BaseCombatAttributes.Select(kvp => new EntityAttribute
            {
                EntityId = source.Id,
                AttributeType = kvp.Key,
                Value = kvp.Value
            }).ToList(),
            ActiveEssenceLoadout = MapActiveLoadout(source)
        };
    }

    private EssenceLoadoutDto? MapActiveLoadout(Character source)
    {
        var loadout = source.EssenceLoadouts.FirstOrDefault(x => x.IsActive);
        if (loadout is null) return null;

        return new EssenceLoadoutDto(
            loadout.Id,
            loadout.Name,
            loadout.IsActive,
            loadout.Slots
                .OrderBy(slot => slot.SlotIndex)
                .Select(MapSlot)
                .ToList());
    }

    private EssenceLoadoutSlotDto MapSlot(EssenceLoadoutSlot slot)
    {
        var definition = slot.PlayerEssence is null
            ? null
            : _essenceDefinitions.GetById(slot.PlayerEssence.EssenceDefinitionId);

        return new(
            slot.SlotIndex,
            slot.PlayerEssenceId,
            slot.PlayerEssence?.EssenceDefinitionId,
            definition?.Name);
    }
}
