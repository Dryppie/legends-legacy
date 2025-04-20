using Application.Common.Mappings;
using Application.UseCases.CharacterActions.Dtos.CombatDtos;
using AutoMapper;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;

namespace Application.UseCases.CharacterActions.Dtos;
public class CharacterActionDto : IMapFrom<CharacterAction>
{
    public CharacterActionType CharacterActionType { get; set; }
    public Guid LootTableId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public CombatSessionDto? CombatSession { get; set; }
    public CombatActionDetails? CombatActionDetails { get; set; }
    public GatheringActionDetails? GatheringActionDetails { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CharacterAction, CharacterActionDto>()
            .ForMember(dest => dest.CombatActionDetails, opt => opt.MapFrom<CombatActionDetailsResolver>())
            .ForMember(dest => dest.GatheringActionDetails, opt => opt.MapFrom<GatheringActionDetailsResolver>());
            //.ForMember(dest => dest.CraftingActionDetails, opt => opt.MapFrom<CraftingActionDetailsResolver>());
    }
}

public class CombatActionDetailsResolver : IValueResolver<CharacterAction, CharacterActionDto, CombatActionDetails?>
{
    public CombatActionDetails? Resolve(CharacterAction source, CharacterActionDto destination, CombatActionDetails? destMember, ResolutionContext context)
    {
        return source.CharacterActionType == CharacterActionType.Combat
            ? context.Mapper.Map<CombatActionDetails>(source.ActionDetails)
            : null;
    }
}

public class GatheringActionDetailsResolver : IValueResolver<CharacterAction, CharacterActionDto, GatheringActionDetails?>
{
    public GatheringActionDetails? Resolve(CharacterAction source, CharacterActionDto destination, GatheringActionDetails? destMember, ResolutionContext context)
    {
        return source.CharacterActionType == CharacterActionType.Gathering
            ? context.Mapper.Map<GatheringActionDetails>(source.ActionDetails)
            : null;
    }
}