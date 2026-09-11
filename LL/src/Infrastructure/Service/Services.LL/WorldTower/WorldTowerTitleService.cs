using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.Services.LL.WorldTower;
using Application.UseCases.Outbox;
using Domain.Models.Achievements;
using Domain.Models.WorldTower;
using Microsoft.Extensions.Options;

namespace Services.LL.WorldTower;

public sealed class WorldTowerTitleService(
    IWorldTowerTitleRepository repository,
    IAchievementRepository titles,
    IAchievementService achievements,
    IWorldTowerDefinitionProvider definitions,
    IGameEventOutbox outbox,
    IOptions<WorldTowerOptions> options,
    TimeProvider timeProvider) : IWorldTowerTitleService
{
    public async Task<TitleDefinition> GetRewardAsync(TowerFloorDefinition floor, CancellationToken cancellationToken)
    {
        var title = await titles.GetActiveTitleByKeyAsync(floor.RewardTitleKey, cancellationToken);
        if (title is null || title.Scope != TitleScope.Character || title.Category != AchievementCategory.WorldTower)
            throw new InvalidOperationException($"World Tower floor {floor.FloorNumber} has an invalid title reward '{floor.RewardTitleKey}'.");
        return title;
    }

    // The caller owns the transaction. Lock all characters in a stable order before other rewards.
    public async Task<int> GrantAsync(TowerFloorDefinition floor, IReadOnlyCollection<TowerTitleRecipient> recipients,
        bool announce, CancellationToken cancellationToken)
    {
        var title = await GetRewardAsync(floor, cancellationToken);
        var granted = 0;
        foreach (var recipient in recipients.DistinctBy(p => p.CharacterId).OrderBy(p => p.CharacterId))
        {
            await repository.LockCharacterAsync(recipient.CharacterId, cancellationToken);
            if (!await achievements.UnlockTitleAsync(recipient.AccountId, recipient.CharacterId, title.Key,
                    JsonSerializer.Serialize(new { Source = "WorldTower", floor.FloorNumber, recipient.AttemptId, recipient.RallyId }),
                    cancellationToken, announce))
                continue;

            granted++;
            if (announce)
                await outbox.EnqueueAsync(GameEventTypes.WorldTowerChatAnnouncement,
                    new WorldTowerChatAnnouncementPayload(recipient.RallyId, Guid.NewGuid(),
                        $"Title unlocked: {title.Name}. You defeated {floor.GuardianName} on World Tower Floor {floor.FloorNumber}.",
                        "/game/character/achievements", timeProvider.GetUtcNow(), recipient.CharacterId),
                    recipient.CharacterId, recipient.AccountId, cancellationToken);
        }
        return granted;
    }

    public async Task<int> BackfillAsync(int floorNumber, CancellationToken cancellationToken)
    {
        var floor = definitions.GetFloor(floorNumber)
            ?? throw new InvalidOperationException($"Unknown World Tower floor {floorNumber}.");
        var title = await GetRewardAsync(floor, cancellationToken);
        var recipients = await repository.GetMissingRecipientsAsync(options.Value.ServerId, floorNumber,
            title.Id, 100, cancellationToken);
        return await GrantAsync(floor, recipients, announce: false, cancellationToken);
    }
}
