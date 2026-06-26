using Application.Interfaces.Services.LL.Achievements;
using Application.MediatR.Markers;
using Application.UseCases.Achievements.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Achievements.Commands.RecalculateAchievements;

public record RecalculateAchievementsCommand(Guid AccountId, Guid CharacterId) : ICommand<Response<AchievementRecalculationResultDto>>;

public sealed class RecalculateAchievementsCommandHandler
    : IRequestHandler<RecalculateAchievementsCommand, Response<AchievementRecalculationResultDto>>
{
    private readonly IAchievementService _achievementService;

    public RecalculateAchievementsCommandHandler(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    public async Task<Response<AchievementRecalculationResultDto>> Handle(
        RecalculateAchievementsCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _achievementService.RecalculateProgressAsync(
            request.AccountId,
            request.CharacterId,
            cancellationToken);

        return result is null
            ? Response<AchievementRecalculationResultDto>.Fail("Character was not found.")
            : Response<AchievementRecalculationResultDto>.Success(result);
    }
}
