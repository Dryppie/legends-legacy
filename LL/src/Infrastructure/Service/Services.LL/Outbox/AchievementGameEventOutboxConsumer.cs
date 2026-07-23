using System.Text.Json;
using Application.Common.Interfaces;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Achievements;
using Application.UseCases.Outbox;
using Domain.Models.Achievements;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Items.Equipments;
using Domain.Models.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Services.LL.Outbox;

public sealed class AchievementGameEventOutboxConsumer(
    IDbContext db,
    IAchievementService achievementService,
    JsonSerializerOptions jsonOptions,
    TimeProvider timeProvider) : IGameEventOutboxConsumer
{
    public string Consumer => GameEventOutboxConsumerNames.Achievements;

    public bool CanHandle(string eventType) =>
        eventType is GameEventTypes.EssenceAbsorbed
            or GameEventTypes.EssenceLoadoutChanged
            or GameEventTypes.EssenceAscended
            or GameEventTypes.EquipmentCrafted
            or GameEventTypes.EquipmentTempered
            or GameEventTypes.BlueprintUnlocked
            or GameEventTypes.IdleCombatEncounterCompleted
            or GameEventTypes.CharacterCreated
            or GameEventTypes.CharacterLevelReached
            or GameEventTypes.DungeonRunStarted
            or GameEventTypes.DungeonRunCompleted
            or GameEventTypes.ColosseumBattleCompleted;

    public async Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        if (await db.AchievementEventLedgers.AnyAsync(
            x => x.OutboxMessageId == message.Id,
            cancellationToken))
        {
            return;
        }

        var handled = await HandleEventAsync(message, cancellationToken);
        if (!handled)
        {
            return;
        }

        db.AchievementEventLedgers.Add(new AchievementEventLedger
        {
            Id = Guid.NewGuid(),
            OutboxMessageId = message.Id,
            CharacterId = message.CharacterId,
            EventType = message.EventType,
            ProcessedAt = timeProvider.GetUtcNow()
        });
    }

    private async Task<bool> HandleEventAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        await (message.EventType switch
        {
            GameEventTypes.EssenceAbsorbed =>
                HandleEssenceAbsorbedAsync(Read<EssenceAbsorbedPayload>(message), cancellationToken),

            GameEventTypes.EssenceLoadoutChanged =>
                HandleEssenceLoadoutChangedAsync(Read<EssenceLoadoutChangedPayload>(message), cancellationToken),

            GameEventTypes.EssenceAscended =>
                HandleEssenceAscendedAsync(Read<EssenceAscendedPayload>(message), cancellationToken),

            GameEventTypes.EquipmentCrafted =>
                HandleEquipmentCraftedAsync(Read<EquipmentCraftedPayload>(message), cancellationToken),

            GameEventTypes.EquipmentTempered =>
                HandleEquipmentTemperedAsync(Read<EquipmentTemperedPayload>(message), cancellationToken),

            GameEventTypes.BlueprintUnlocked =>
                HandleBlueprintUnlockedAsync(Read<BlueprintUnlockedPayload>(message), cancellationToken),

            GameEventTypes.IdleCombatEncounterCompleted =>
                HandleIdleCombatAsync(Read<IdleCombatEncounterCompletedPayload>(message), cancellationToken),

            GameEventTypes.CharacterCreated =>
                HandleCharacterCreatedAsync(Read<CharacterCreatedPayload>(message), cancellationToken),

            GameEventTypes.CharacterLevelReached =>
                HandleCharacterLevelReachedAsync(Read<CharacterLevelReachedPayload>(message), cancellationToken),

            GameEventTypes.DungeonRunStarted =>
                HandleDungeonRunStartedAsync(Read<DungeonRunStartedPayload>(message), cancellationToken),

            GameEventTypes.DungeonRunCompleted =>
                HandleDungeonRunCompletedAsync(Read<DungeonRunCompletedPayload>(message), cancellationToken),

            GameEventTypes.ColosseumBattleCompleted =>
                HandleColosseumBattleCompletedAsync(Read<ColosseumBattleCompletedPayload>(message), cancellationToken),

            _ => Task.CompletedTask
        });

        return CanHandle(message.EventType);
    }

    private Task HandleEssenceAbsorbedAsync(
        EssenceAbsorbedPayload payload,
        CancellationToken cancellationToken) =>
        achievementService.RecordEssenceAbsorbedAsync(
            payload.CharacterId,
            payload.UniqueEssenceCount,
            payload.CompletedCollectionKeys,
            cancellationToken);

    private Task HandleEssenceLoadoutChangedAsync(
        EssenceLoadoutChangedPayload payload,
        CancellationToken cancellationToken) =>
        achievementService.RecordEssenceLoadoutSavedAsync(
            payload.CharacterId,
            payload.EquippedEssenceCount,
            cancellationToken);

    private Task HandleEssenceAscendedAsync(
        EssenceAscendedPayload payload,
        CancellationToken cancellationToken) =>
        achievementService.RecordEssenceAscendedAsync(
            payload.CharacterId,
            payload.AscensionTier,
            payload.AscendedToTierCount,
            cancellationToken);

    private Task HandleEquipmentCraftedAsync(
        EquipmentCraftedPayload payload,
        CancellationToken cancellationToken) =>
        achievementService.RecordItemsCraftedAsync(
            payload.CharacterId,
            payload.CraftedItems.Select(ToEquipmentInstance).ToList(),
            cancellationToken);

    private Task HandleEquipmentTemperedAsync(
        EquipmentTemperedPayload payload,
        CancellationToken cancellationToken) =>
        achievementService.RecordItemsTemperedAsync(
            payload.CharacterId,
            payload.Summary ?? new TemperingSummary(),
            payload.CompletedItems.Select(ToEquipmentInstance).ToList(),
            cancellationToken);

    private Task HandleBlueprintUnlockedAsync(
        BlueprintUnlockedPayload payload,
        CancellationToken cancellationToken) =>
        achievementService.RecordBlueprintUnlockedAsync(payload.CharacterId, cancellationToken);

    private Task HandleIdleCombatAsync(
        IdleCombatEncounterCompletedPayload payload,
        CancellationToken cancellationToken) =>
        achievementService.RecordIdleCombatAsync(
            payload.CharacterId,
            payload.MonstersDefeated,
            payload.DefeatedCreatureFamilyKeys,
            payload.PlayerDefeats,
            payload.LowestWinningHealthPercent,
            cancellationToken);

    private Task HandleCharacterCreatedAsync(
        CharacterCreatedPayload payload,
        CancellationToken cancellationToken) =>
        achievementService.RecordCharacterCreatedAsync(payload.CharacterId, cancellationToken);

    private Task HandleCharacterLevelReachedAsync(
        CharacterLevelReachedPayload payload,
        CancellationToken cancellationToken) =>
        achievementService.RecordCharacterLevelReachedAsync(
            payload.CharacterId,
            payload.Level,
            cancellationToken);

    private Task HandleDungeonRunStartedAsync(
        DungeonRunStartedPayload payload,
        CancellationToken cancellationToken) =>
        achievementService.RecordDungeonRunStartedAsync(payload.CharacterId, cancellationToken);

    private Task HandleDungeonRunCompletedAsync(
        DungeonRunCompletedPayload payload,
        CancellationToken cancellationToken) =>
        achievementService.RecordDungeonRunCompletedAsync(
            payload.CharacterId,
            payload.DungeonDefinitionId,
            payload.CompletedWithoutDefeat,
            payload.CompletedWithoutRetreat,
            payload.DefeatedBossKeys,
            cancellationToken);

    private Task HandleColosseumBattleCompletedAsync(
        ColosseumBattleCompletedPayload payload,
        CancellationToken cancellationToken) =>
        achievementService.RecordColosseumBattleAsync(
            payload.CharacterId,
            payload.OpponentCharacterId,
            payload.Outcome,
            payload.CharacterRatingBefore,
            payload.OpponentRatingBefore,
            cancellationToken);

    private static EquipmentInstance ToEquipmentInstance(OutboxEquipmentItemPayload item) =>
        new()
        {
            ItemBaseId = item.ItemBaseId,
            Tier = item.Tier,
            Rarity = item.Rarity,
            Quality = item.Quality,
            Potential = item.Potential,
            BaseRecipeId = item.BaseRecipeId,
            BlueprintId = item.BlueprintId,
            AffinityTags = item.AffinityTags.ToList(),
            IsMasterpiece = item.IsMasterpiece
        };

    private T Read<T>(GameEventOutboxMessage message) =>
        JsonSerializer.Deserialize<T>(message.PayloadJson, jsonOptions)
        ?? throw new InvalidOperationException(
            $"Outbox message '{message.Id}' could not be deserialized as {typeof(T).Name}.");
}
