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

    async Task<IReadOnlyList<IReadOnlyList<InventoryItem>>> RollEssenceDropGroupsAsync(
        Guid characterId,
        IReadOnlyList<IReadOnlyList<Creature>> defeatedCreatureGroups,
        bool eligible,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<BonusKind, double>? bonusFactors = null,
        EssenceDropRollModifiers? modifiers = null)
    {
        var groups = new List<IReadOnlyList<InventoryItem>>(defeatedCreatureGroups.Count);
        foreach (var defeatedCreatures in defeatedCreatureGroups)
        {
            groups.Add(await RollEssenceDropsAsync(
                characterId,
                defeatedCreatures,
                eligible,
                cancellationToken,
                bonusFactors,
                modifiers));
        }

        return groups;
    }
}

public sealed record EssenceDropRollResult(bool Dropped, string? EssenceDefinitionId, double EffectiveDropChance, double ResonanceValue);

public sealed record EssenceDropRollModifiers(
    double DropChanceMultiplier = 1,
    double PityProgressionMultiplier = 1,
    double ResonanceCapMultiplier = 1);
