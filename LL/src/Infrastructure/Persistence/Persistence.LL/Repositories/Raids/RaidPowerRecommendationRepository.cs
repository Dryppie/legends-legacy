using System.Text.Json;
using Application.Interfaces.Services.LL.Raids;
using Domain.Models.Raids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Persistence.LL.Repositories.Raids;

public sealed class RaidPowerRecommendationRepository(
    LLDbContext context,
    ILogger<RaidPowerRecommendationRepository> logger)
    : IRaidPowerRecommendationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<PersistedRaidPowerRecommendation>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var entries = await context.RaidPowerRecommendationCacheEntries
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        var output = new List<PersistedRaidPowerRecommendation>(entries.Length);
        foreach (var entry in entries)
        {
            try
            {
                var recommendation = JsonSerializer.Deserialize<RaidPowerRecommendation>(
                    entry.RecommendationJson,
                    JsonOptions);
                if (recommendation is null)
                    continue;
                output.Add(new PersistedRaidPowerRecommendation(
                    new RaidPowerCalibrationIdentity(
                        entry.RaidBossId,
                        entry.Tier,
                        entry.DefinitionHash,
                        entry.RaidRulesVersion,
                        entry.PowerRatingAlgorithmVersion,
                        entry.CombatRulesVersion,
                        entry.EquipmentBalanceVersion,
                        entry.SeedSetVersion),
                    recommendation,
                    entry.UpdatedAtUtc));
            }
            catch (JsonException exception)
            {
                logger.LogWarning(
                    exception,
                    "Ignoring invalid persisted raid power recommendation for {RaidBossId} tier {Tier}.",
                    entry.RaidBossId,
                    entry.Tier);
            }
        }
        return output;
    }

    public async Task UpsertAsync(
        PersistedRaidPowerRecommendation persisted,
        CancellationToken cancellationToken)
    {
        var identity = persisted.Identity;
        var entry = await context.RaidPowerRecommendationCacheEntries.SingleOrDefaultAsync(
            x => x.RaidBossId == identity.RaidBossId && x.Tier == identity.Tier,
            cancellationToken);
        if (entry is null)
        {
            entry = new RaidPowerRecommendationCacheEntry
            {
                RaidBossId = identity.RaidBossId,
                Tier = identity.Tier
            };
            context.RaidPowerRecommendationCacheEntries.Add(entry);
        }

        entry.DefinitionHash = identity.DefinitionHash;
        entry.RaidRulesVersion = identity.RaidRulesVersion;
        entry.PowerRatingAlgorithmVersion = identity.PowerRatingAlgorithmVersion;
        entry.CombatRulesVersion = identity.CombatRulesVersion;
        entry.EquipmentBalanceVersion = identity.EquipmentBalanceVersion;
        entry.SeedSetVersion = identity.SeedSetVersion;
        entry.RecommendationJson = JsonSerializer.Serialize(persisted.Recommendation, JsonOptions);
        entry.UpdatedAtUtc = persisted.UpdatedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
    }
}
