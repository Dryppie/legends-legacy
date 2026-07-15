using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Prophecies;
using AutoMapper;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class PropheciesOverviewDto : IMapFrom<PropheciesOverview>
{
    public DateTimeOffset ServerTime { get; set; }
    public int DailyRerollsRemaining { get; set; }
    public int DailyRerollsUsed { get; set; }
    public int DailyRerollLimit { get; set; }
    public int? NextDailyRerollCost { get; set; }
    public long FateEcho { get; set; }
    public List<ProphecyInstanceDto> DailyProphecies { get; set; } = [];
    public ProphecyInstanceDto? ActiveDailyProphecy { get; set; }
    public ProphecyInstanceDto GreaterProphecy { get; set; } = new();
    public WeeklyRevelationProgressDto WeeklyRevelation { get; set; } = new();
    public List<ProphecyInstanceDto> RecentProphecies { get; set; } = [];
    public List<ProphecyCacheInventoryDto> Caches { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<PropheciesOverview, PropheciesOverviewDto>()
            .ForMember(
                dest => dest.WeeklyRevelation,
                opt => opt.MapFrom((src, _, _, context) =>
                    ProphecyMappingHelpers.MapWeeklyRevelation(src.WeeklyRevelation, src.WeeklyMilestones, context)));
    }
}
