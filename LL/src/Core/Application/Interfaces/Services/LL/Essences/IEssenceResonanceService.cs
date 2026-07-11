namespace Application.Interfaces.Services.LL.Essences;

using Domain.Models.Bonuses;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;

public interface IEssenceResonanceService
{
    Task<EssenceDropRollResult> RollMonsterEssenceDropAsync(Guid characterId, string monsterId, bool eligible, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryItem>> RollEssenceDropsAsync(
        Guid characterId,
        IReadOnlyList<Creature> defeatedCreatures,
        bool eligible,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<BonusKind, double>? bonusFactors = null);
}

public sealed record EssenceDropRollResult(bool Dropped, string? EssenceDefinitionId, double EffectiveDropChance, double ResonanceValue);
