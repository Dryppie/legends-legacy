using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Prophecies;
using AutoMapper;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class WeeklyRevelationMilestoneDto : IMapFrom<WeeklyRevelationMilestone>
{
    public int FavorRequired { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; }
    public bool IsClaimed { get; set; }
    public ProphecyRewardSnapshotDto Reward { get; set; } = new();

    public void Mapping(Profile profile)
    {
        profile.CreateMap<WeeklyRevelationMilestone, WeeklyRevelationMilestoneDto>();
    }
}
