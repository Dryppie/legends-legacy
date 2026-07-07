using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Items;

namespace Application.UseCases.Outbox;

public sealed record EquipmentChangedPayload(Guid CharacterId);

public sealed record EssenceAbsorbedPayload(
    Guid CharacterId,
    string EssenceDefinitionId,
    int UniqueEssenceCount,
    IReadOnlyCollection<string> CompletedCollectionKeys);

public sealed record EssenceLoadoutChangedPayload(
    Guid CharacterId,
    IReadOnlyCollection<Guid> AttunedPlayerEssenceIds,
    int EquippedEssenceCount);

public sealed record EssenceAscendedPayload(
    Guid CharacterId,
    int AscensionTier,
    int AscendedToTierCount);

public sealed record EquipmentCraftedPayload(
    Guid CharacterId,
    IReadOnlyCollection<OutboxEquipmentItemPayload> CraftedItems);

public sealed record EquipmentTemperedPayload(
    Guid CharacterId,
    TemperingSummary Summary,
    IReadOnlyCollection<OutboxEquipmentItemPayload> CompletedItems);

public sealed record BlueprintUnlockedPayload(Guid CharacterId);

public sealed record IdleCombatEncounterCompletedPayload(
    Guid CharacterId,
    string AreaId,
    bool WonEncounter,
    int MonstersDefeated,
    IReadOnlyCollection<string> DefeatedCreatureFamilyKeys,
    int PlayerDefeats,
    int? LowestWinningHealthPercent);

public sealed record CharacterCreatedPayload(Guid CharacterId);

public sealed record CharacterLevelReachedPayload(
    Guid CharacterId,
    int Level);

public sealed record DungeonRunStartedPayload(Guid CharacterId);

public sealed record DungeonRunCompletedPayload(
    Guid CharacterId,
    string DungeonDefinitionId,
    bool CompletedWithoutDefeat,
    bool CompletedWithoutCheckpointRetreat,
    IReadOnlyCollection<string> DefeatedBossKeys);

public sealed record ColosseumBattleCompletedPayload(
    Guid CharacterId,
    Guid OpponentCharacterId,
    BattleOutcome Outcome,
    int CharacterRatingBefore,
    int OpponentRatingBefore);

public sealed record ClientTutorialStepPayload(
    Guid CharacterId,
    string StepKey,
    string TriggerType,
    string? Route);

public sealed record OutboxEquipmentItemPayload(
    string ItemBaseId,
    int Tier,
    Rarity Rarity,
    ItemQuality Quality,
    int? Potential,
    string? RecipeId,
    string? BaseRecipeId,
    string? BlueprintId,
    IReadOnlyCollection<string> AffinityTags,
    IReadOnlyCollection<string> SpecialModifiers,
    bool IsMasterpiece);
