using Application.Interfaces.Services.LL.Prophecies;
using AutoMapper;
using Domain.Models.Prophecies;
using System.Text.Json;

namespace Application.UseCases.Prophecies.Dtos;

internal static class ProphecyMappingHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ProphecyDefinition Definition(PlayerProphecyInstance instance) =>
        instance.ProphecyDefinition ?? new ProphecyDefinition
        {
            Id = instance.ProphecyDefinitionId,
            Title = instance.ProphecyDefinitionId,
            FlavorText = string.Empty,
            ObjectiveText = string.Empty,
            ObjectiveType = string.Empty
        };

    public static ProphecyRewardSnapshot ReadReward(string json) =>
        JsonSerializer.Deserialize<ProphecyRewardSnapshot>(json, JsonOptions) ?? new ProphecyRewardSnapshot();

    public static WeeklyRevelationProgressDto MapWeeklyRevelation(
        WeeklyRevelationProgress progress,
        IReadOnlyList<WeeklyRevelationMilestone> milestones,
        ResolutionContext context)
    {
        var dto = context.Mapper.Map<WeeklyRevelationProgressDto>(progress);
        dto.Milestones = milestones
            .Select(context.Mapper.Map<WeeklyRevelationMilestoneDto>)
            .ToList();

        return dto;
    }
}
