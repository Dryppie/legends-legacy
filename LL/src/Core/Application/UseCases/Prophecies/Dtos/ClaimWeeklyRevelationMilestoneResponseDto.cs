using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Prophecies;
using AutoMapper;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class ClaimWeeklyRevelationMilestoneResponseDto : IMapFrom<WeeklyRevelationClaimResult>
{
    public int FavorRequired { get; set; }
    public ProphecyRewardSnapshotDto Reward { get; set; } = new();
    public WeeklyRevelationProgressDto WeeklyRevelation { get; set; } = new();

    public void Mapping(Profile profile)
    {
        profile.CreateMap<WeeklyRevelationClaimResult, ClaimWeeklyRevelationMilestoneResponseDto>()
            .ForMember(
                dest => dest.WeeklyRevelation,
                opt => opt.MapFrom((src, _, _, context) =>
                    ProphecyMappingHelpers.MapWeeklyRevelation(src.WeeklyRevelation, src.WeeklyMilestones, context)));
    }
}
