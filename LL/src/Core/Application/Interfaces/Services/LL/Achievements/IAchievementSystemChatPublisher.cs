using Application.UseCases.Achievements.Dtos;

namespace Application.Interfaces.Services.LL.Achievements;

public interface IAchievementSystemChatPublisher
{
    Task PublishAsync(
        Guid? characterId,
        IReadOnlyCollection<AchievementUnlockDto> unlocks,
        CancellationToken cancellationToken);
}
