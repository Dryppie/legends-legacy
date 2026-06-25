namespace Application.UseCases.Prophecies.Dtos;

public sealed record ClaimWeeklyRevelationMilestoneResponseDto(
    int FavorRequired,
    ProphecyRewardSnapshotDto Reward,
    WeeklyRevelationProgressDto WeeklyRevelation);
