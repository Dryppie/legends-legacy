using Application.Interfaces.Services.LL.Guilds;
using Domain.Models.Guilds.Missions;
using Application.Interfaces.Services.LL.CombatStyles;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class IdleCombatRewardApplier : IIdleCombatRewardApplier
{
    private readonly IExperienceRewardWriter _experienceWriter;
    private readonly ILootRewardWriter _lootWriter;
    private readonly ICurrencyRewardWriter _currencyWriter;
    private readonly IGuildMissionService _guildMissionService;
    private readonly ICombatStyleService _combatStyleService;

    public IdleCombatRewardApplier(
        IExperienceRewardWriter experienceWriter,
        ILootRewardWriter lootWriter,
        ICurrencyRewardWriter currencyWriter,
        ICombatStyleService combatStyleService,
        IGuildMissionService guildMissionService)
    {
        _experienceWriter = experienceWriter;
        _lootWriter = lootWriter;
        _currencyWriter = currencyWriter;
        _guildMissionService = guildMissionService;
        _combatStyleService = combatStyleService;
    }

    public async Task ApplyAsync(
        IdleCombatRewardFacts facts,
        IdleCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken)
    {
        if (outcome.TotalExperience > 0)
        {
            await _experienceWriter.AddSplitExperienceAsync(
                facts.PlayerEntityIds,
                outcome.TotalExperience,
                cancellationToken);

            await _combatStyleService.GrantExperienceAsync(
                facts.CharacterId,
                outcome.TotalExperience,
                "idle_combat",
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

        var creaturesDefeated = facts.Encounters
            .Where(x => x.IsVictory)
            .Sum(x => x.HostileCreatures.Count);
        if (creaturesDefeated > 0)
        {
            await _guildMissionService.RecordContributionAsync(
                new GuildContributionEvent(
                    facts.CharacterId,
                    GuildContributionSource.Combat,
                    GuildContributionMetric.CreaturesDefeated,
                    creaturesDefeated,
                    OccurredAt: facts.ProcessedUntil,
                    IdempotencyKey: $"idle-combat:{facts.CharacterId}:{facts.From:O}:{facts.ProcessedUntil:O}:{creaturesDefeated}"),
                cancellationToken);
        }
    }
}
