using Application.Common.Mappings;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Application.UseCases.CharacterActions.Dtos.Responses.CraftingDtos;
using AutoMapper;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;

namespace Application.UseCases.CharacterActions.Dtos.Responses;
public class CharacterActionDto : IMapFrom<CharacterAction>
{
    public CharacterActionType CharacterActionType { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? NextResolutionAtUtc { get; set; }
    public DateTimeOffset? BlockedUntilUtc { get; set; }
    public long ScheduleGeneration { get; set; }
    public int ProcessedCount { get; set; }
    public bool HasMoreDueWork { get; set; }
    public int? ResolutionIntervalMs { get; set; }
    public bool IsDeleted { get; set; }
    public bool AutoResumedFromTempering { get; set; }
    public string? ReturnToCombatAreaId { get; set; }
    public CombatSessionDto? CombatSession { get; set; }
    public TemperingSessionDto? TemperingSession { get; set; }
    public CombatActionDetails? CombatActionDetails { get; set; }
    public CraftingActionDetailsDto? CraftingActionDetails { get; set; }
    public List<CraftingQueueItemDto> TemperingQueueItems { get; set; } = [];

    public string Revision => string.Join(':',
        ScheduleGeneration,
        CombatSession?.CombatResult?.StartedAt.UtcDateTime.Ticks ?? NextResolutionAtUtc?.UtcDateTime.Ticks ?? UpdatedAt.UtcDateTime.Ticks,
        NextResolutionAtUtc?.UtcDateTime.Ticks ?? 0,
        BlockedUntilUtc?.UtcDateTime.Ticks ?? 0,
        UpdatedAt.UtcDateTime.Ticks,
        IsDeleted);

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CharacterAction, CharacterActionDto>()
            .ForMember(dest => dest.CombatActionDetails, opt => opt.MapFrom<CombatActionDetailsResolver>())
            .ForMember(dest => dest.CraftingActionDetails, opt => opt.MapFrom<CraftingActionDetailsResolver>())
            .ForMember(dest => dest.TemperingQueueItems, opt => opt.MapFrom<TemperingQueueItemsResolver>());
    }
}

public class TemperingQueueItemsResolver : IValueResolver<CharacterAction, CharacterActionDto, List<CraftingQueueItemDto>>
{
    public List<CraftingQueueItemDto> Resolve(
        CharacterAction source,
        CharacterActionDto destination,
        List<CraftingQueueItemDto> destMember,
        ResolutionContext context)
    {
        var queue = source.ActionDetails is CraftingActionDetails craftingDetails
            ? craftingDetails.CraftingQueueItems
            : source.PausedTemperingQueueItems;

        return context.Mapper.Map<List<CraftingQueueItemDto>>(queue
            .OrderBy(item => item.Position)
            .ThenBy(item => item.AddedAt)
            .ThenBy(item => item.Id));
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

public class CraftingActionDetailsResolver : IValueResolver<CharacterAction, CharacterActionDto, CraftingActionDetailsDto?>
{
    public CraftingActionDetailsDto? Resolve(CharacterAction source, CharacterActionDto destination, CraftingActionDetailsDto? destMember, ResolutionContext context)
    {
        return source.CharacterActionType == CharacterActionType.Crafting
            ? context.Mapper.Map<CraftingActionDetailsDto>(source.ActionDetails)
            : null;
    }
}
