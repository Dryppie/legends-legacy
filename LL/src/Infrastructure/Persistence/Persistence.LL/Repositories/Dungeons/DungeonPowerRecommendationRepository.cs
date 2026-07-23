using System.Text.Json;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Dungeons.PowerRatings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Persistence.LL.Repositories.Dungeons;

public sealed class DungeonPowerRecommendationRepository(
    LLDbContext context,
    ILogger<DungeonPowerRecommendationRepository> logger)
    : IDungeonPowerRecommendationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<PersistedDungeonPowerRecommendation>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var entries = await context.DungeonPowerRecommendationCacheEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var recommendations = new List<PersistedDungeonPowerRecommendation>(entries.Count);

        foreach (var entry in entries)
        {
            try
            {
                var recommendation = JsonSerializer.Deserialize<DungeonPowerRecommendation>(
                    entry.RecommendationJson,
                    JsonOptions);
                if (recommendation is null)
                    continue;

                recommendations.Add(new PersistedDungeonPowerRecommendation(
                    new DungeonPowerCalibrationIdentity(
                        entry.DungeonId,
                        entry.DungeonTier,
                        entry.DungeonContentHash,
                        entry.AlgorithmVersion,
                        entry.CombatRulesVersion,
                        entry.BenchmarkDefinitionVersion,
                        entry.RecommendationSeedSetVersion),
                    recommendation,
                    entry.UpdatedAtUtc));
            }
            catch (JsonException exception)
            {
                logger.LogWarning(
                    exception,
                    "Ignoring invalid persisted Power recommendation for dungeon {DungeonId}.",
                    entry.DungeonId);
            }
        }

        return recommendations;
    }

    public async Task UpsertAsync(
        PersistedDungeonPowerRecommendation persisted,
        CancellationToken cancellationToken)
    {
        var identity = persisted.Identity;
        var entry = await context.DungeonPowerRecommendationCacheEntries
            .SingleOrDefaultAsync(x => x.DungeonId == identity.DungeonId, cancellationToken);
        if (entry is null)
        {
            entry = new DungeonPowerRecommendationCacheEntry { DungeonId = identity.DungeonId };
            context.DungeonPowerRecommendationCacheEntries.Add(entry);
        }

        entry.DungeonTier = identity.DungeonTier;
        entry.DungeonContentHash = identity.DungeonContentHash;
        entry.AlgorithmVersion = identity.AlgorithmVersion;
        entry.CombatRulesVersion = identity.CombatRulesVersion;
        entry.BenchmarkDefinitionVersion = identity.BenchmarkDefinitionVersion;
        entry.RecommendationSeedSetVersion = identity.RecommendationSeedSetVersion;
        entry.RecommendationJson = JsonSerializer.Serialize(persisted.Recommendation, JsonOptions);
        entry.UpdatedAtUtc = persisted.UpdatedAtUtc;

        await context.SaveChangesAsync(cancellationToken);
    }
}
