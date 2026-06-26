using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Prophecies;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class WeeklyRevelationProgressDto : IMapFrom<WeeklyRevelationProgress>
{
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public int PropheticFavor { get; set; }
    public List<WeeklyRevelationMilestoneDto> Milestones { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<WeeklyRevelationProgress, WeeklyRevelationProgressDto>();
    }
}
