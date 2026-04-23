using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

internal class DungeonCombatRewardApplier : IDungeonCombatRewardApplier
{
    private readonly IExperienceRewardWriter _experienceWriter;
    private readonly ILootRewardWriter _lootWriter;
    private readonly ICurrencyRewardWriter _currencyWriter;

    public DungeonCombatRewardApplier(
        IExperienceRewardWriter experienceWriter,
        ILootRewardWriter lootWriter,
        ICurrencyRewardWriter currencyWriter)
    {
        _experienceWriter = experienceWriter;
        _lootWriter = lootWriter;
        _currencyWriter = currencyWriter;
    }

    public async Task ApplyAsync(
        DungeonCombatRewardFacts facts,
        DungeonCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken)
    {
        if (outcome.TotalExperience > 0)
        {
            await _experienceWriter.AddSplitExperienceAsync(
                facts.PlayerEntityIds,
                outcome.TotalExperience,
                cancellationToken);
        }

        if (outcome.TotalLoot.Count > 0)
        {
            await _lootWriter.AddLootAsync(
                facts.CharacterId,
                outcome.TotalLoot,
                cancellationToken);
        }

        if (outcome.TotalCinders > 0 || outcome.TotalSoulstones > 0)
        {
            await _currencyWriter.AddAsync(
                facts.CharacterId,
                outcome.TotalCinders,
                outcome.TotalSoulstones,
                cancellationToken);
        }
    }
}