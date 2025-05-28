using Application.Common.Mappings;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Application.UseCases.CharacterActions.Dtos.Responses.CraftingDtos;
using Application.UseCases.CharacterActions.Dtos.Responses.GatheringDtos;
using AutoMapper;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;

namespace Application.UseCases.CharacterActions.Dtos.Responses;
public class CharacterActionDto : IMapFrom<CharacterAction>
{
    public CharacterActionType CharacterActionType { get; set; }
    public Guid LootTableId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public CombatSessionDto? CombatSession { get; set; }
    public TemperingSessionDto? TemperingSession { get; set; }
    public GatheringSessionDto? GatheringSession { get; set; }
    public CombatActionDetails? CombatActionDetails { get; set; }
    public GatheringActionDetails? GatheringActionDetails { get; set; }
    public CraftingActionDetailsDto? CraftingActionDetails { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CharacterAction, CharacterActionDto>()
            .ForMember(dest => dest.CombatActionDetails, opt => opt.MapFrom<CombatActionDetailsResolver>())
            .ForMember(dest => dest.GatheringActionDetails, opt => opt.MapFrom<GatheringActionDetailsResolver>())
            .ForMember(dest => dest.CraftingActionDetails, opt => opt.MapFrom<CraftingActionDetailsResolver>());
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
public class CraftingActionDetailsResolver : IValueResolver<CharacterAction, CharacterActionDto, CraftingActionDetailsDto?>
{
    public CraftingActionDetailsDto? Resolve(CharacterAction source, CharacterActionDto destination, CraftingActionDetailsDto? destMember, ResolutionContext context)
    {
        return source.CharacterActionType == CharacterActionType.Crafting
            ? context.Mapper.Map<CraftingActionDetailsDto>(source.ActionDetails)
            : null;
    }
}