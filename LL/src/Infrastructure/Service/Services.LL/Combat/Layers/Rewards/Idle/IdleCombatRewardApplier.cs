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
    private readonly Application.Interfaces.Services.LL.Nobility.INobilityService? _nobility;

    public IdleCombatRewardApplier(
        IExperienceRewardWriter experienceWriter,
        ILootRewardWriter lootWriter,
        ICurrencyRewardWriter currencyWriter,
        IGuildMissionService guildMissionService,
        Application.Interfaces.Services.LL.Nobility.INobilityService? nobility = null)
    {
        _experienceWriter = experienceWriter;
        _lootWriter = lootWriter;
        _currencyWriter = currencyWriter;
        _guildMissionService = guildMissionService;
        _nobility = nobility;
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
        using var entitlementTime = _nobility?.EvaluateCombatAt(facts.From);
        if (outcome.TotalExperience > 0 && facts.PlayerEntityIds.Count > 0)
        {
            var recipients = facts.PlayerEntityIds.Distinct().ToArray();
            if (recipients.Length <= 1)
            {
                await _experienceWriter.AddSplitExperienceAsync(
                    facts.PlayerEntityIds,
                    outcome.TotalExperience,
                    EssenceCombatActivity.IdleCombat,
                    cancellationToken);
            }
            else
            {
                var shares = new int[recipients.Length];
                // Split each encounter before batching, so offline and online rewards
                // give the same recipients the remainders. Persist each recipient once.
                foreach (var encounter in outcome.EncounterOutcomes)
                {
                    var baseShare = encounter.ExperienceGained / recipients.Length;
                    var remainder = encounter.ExperienceGained % recipients.Length;
                    for (var index = 0; index < recipients.Length; index++)
                    {
                        shares[index] = checked(shares[index] + baseShare + (index < remainder ? 1 : 0));
                    }
                }

                for (var index = 0; index < recipients.Length; index++)
                {
                    if (shares[index] <= 0) continue;
                    await _experienceWriter.AddSplitExperienceAsync(
                        [recipients[index]], shares[index], EssenceCombatActivity.IdleCombat, cancellationToken);
                }
            }
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
