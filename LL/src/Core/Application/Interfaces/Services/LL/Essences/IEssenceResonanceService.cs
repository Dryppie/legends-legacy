namespace Application.Interfaces.Services.LL.Essences;

using Domain.Models.Bonuses;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;

public interface IEssenceResonanceService
{
    Task PrepareEssenceDropsAsync(
        Guid characterId,
        IReadOnlyList<Creature> defeatedCreatures,
        bool loadEssenceFocus,
        CancellationToken cancellationToken);
    Task<EssenceDropRollResult> RollMonsterEssenceDropAsync(
        Guid characterId,
        string monsterId,
        bool eligible,
        CancellationToken cancellationToken,
        EssenceDropRollModifiers? modifiers = null);
    Task<IReadOnlyList<InventoryItem>> RollEssenceDropsAsync(
        Guid characterId,
        IReadOnlyList<Creature> defeatedCreatures,
        bool eligible,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<BonusKind, double>? bonusFactors = null,
        EssenceDropRollModifiers? modifiers = null);
}

public sealed record EssenceDropRollResult(bool Dropped, string? EssenceDefinitionId, double EffectiveDropChance, double ResonanceValue);

public sealed record EssenceDropRollModifiers(
    double DropChanceMultiplier = 1,
    double PityProgressionMultiplier = 1,
    double ResonanceCapMultiplier = 1);
