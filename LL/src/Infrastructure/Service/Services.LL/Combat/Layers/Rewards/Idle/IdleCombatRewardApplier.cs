using Application.Interfaces.Services.LL.Guilds;
using Domain.Models.Guilds.Missions;
using Domain.Models.Essences;
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

    public IdleCombatRewardApplier(
        IExperienceRewardWriter experienceWriter,
        ILootRewardWriter lootWriter,
        ICurrencyRewardWriter currencyWriter,
        IGuildMissionService guildMissionService)
    {
        _experienceWriter = experienceWriter;
        _lootWriter = lootWriter;
        _currencyWriter = currencyWriter;
        _guildMissionService = guildMissionService;
    }

    public async Task ApplyAsync(
        IdleCombatRewardFacts facts,
        IdleCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken)
    {
        await ApplyProgressionAsync(facts, outcome, cancellationToken);
        await ApplySettlementAsync(
            [CreateSettlementBatch(facts, outcome)],
            cancellationToken);
    }

    public async Task ApplyProgressionAsync(
        IdleCombatRewardFacts facts,
        IdleCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken)
    {
        if (outcome.TotalExperience > 0)
        {
            await _experienceWriter.AddSplitExperienceAsync(
                facts.PlayerEntityIds,
                outcome.TotalExperience,
                EssenceCombatActivity.IdleCombat,
                cancellationToken);
        }

        // Guild missions are evaluated at the original checkpoint timestamp. Keep
        // this operation at every semantic batch so a catch-up crossing a mission
        // period boundary produces exactly the same contribution as before.
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

    public async Task ApplySettlementAsync(
        IReadOnlyList<IdleCombatSettlementBatch> batches,
        CancellationToken cancellationToken)
    {
        if (batches.Count == 0)
        {
            return;
        }

        var characterId = batches[0].CharacterId;
        if (batches.Any(batch => batch.CharacterId != characterId))
        {
            throw new InvalidOperationException(
                "Idle combat settlement batches must target one character.");
        }

        var totalLoot = batches
            .SelectMany(batch => batch.Loot)
            .ToArray();

        if (totalLoot.Length > 0)
        {
            await _lootWriter.AddLootAsync(
                characterId,
                totalLoot,
                "combat-reward",
                batches[^1].AreaName,
                cancellationToken);
        }

        var totalCinders = checked(batches.Sum(batch => batch.Cinders));
        var totalSoulstones = checked(batches.Sum(batch => batch.Soulstones));
        if (totalCinders > 0 || totalSoulstones > 0)
        {
            await _currencyWriter.AddAsync(
                characterId,
                totalCinders,
                totalSoulstones,
                cancellationToken);
        }
    }

    private static IdleCombatSettlementBatch CreateSettlementBatch(
        IdleCombatRewardFacts facts,
        IdleCombatCalculatedOutcome outcome) =>
        new(
            facts.CharacterId,
            facts.From,
            facts.ProcessedUntil,
            facts.Area.Id,
            facts.Area.Name,
            outcome.TotalLoot,
            outcome.TotalCinders,
            outcome.TotalSoulstones,
            [],
            [],
            0,
            null,
            facts.Encounters.Count,
            facts.Encounters.Count(x => x.IsVictory),
            []);
}
