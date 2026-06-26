using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Prophecies;
using AutoMapper;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class ProphecyClaimResponseDto : IMapFrom<ProphecyClaimResult>
{
    public ProphecyInstanceDto Prophecy { get; set; } = new();
    public ProphecyRewardSnapshotDto Reward { get; set; } = new();
    public WeeklyRevelationProgressDto WeeklyRevelation { get; set; } = new();

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ProphecyClaimResult, ProphecyClaimResponseDto>()
            .ForMember(
                dest => dest.WeeklyRevelation,
                opt => opt.MapFrom((src, _, _, context) =>
                    ProphecyMappingHelpers.MapWeeklyRevelation(src.WeeklyRevelation, src.WeeklyMilestones, context)));
    }
}
