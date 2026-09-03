using Application.Common.Mappings;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
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
    public CombatSessionDto? CombatSession { get; set; }
    public CombatActionDetails? CombatActionDetails { get; set; }

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
            .ForMember(dest => dest.CombatActionDetails, opt => opt.MapFrom<CombatActionDetailsResolver>());
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

