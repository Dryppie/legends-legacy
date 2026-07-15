using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Prophecies;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Prophecies;

public sealed class JsonProphecyBalanceProvider : IProphecyBalanceProvider
{
    private static readonly int[] PersistedMilestoneThresholds = [3, 5, 7];
    private readonly ProphecyBalanceCatalog _catalog;

    public JsonProphecyBalanceProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options,
        IProphecyDefinitionProvider definitionProvider)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "prophecies");

        var targetDocument = Read<TargetDocument>(Path.Combine(path, "targets.json"), options);
        var rewardDocument = Read<RewardDocument>(Path.Combine(path, "rewards.json"), options);
        var revelationDocument = Read<WeeklyRevelationDocument>(Path.Combine(path, "weekly-revelation.json"), options);
        var cacheDocument = Read<CacheDocument>(Path.Combine(path, "caches.json"), options);
        var economyDocument = Read<EconomyDocument>(Path.Combine(path, "economy.json"), options);

        _catalog = new ProphecyBalanceCatalog
        {
            Targets = targetDocument.Targets,
            RewardScaling = rewardDocument.Scaling,
            RewardProfiles = rewardDocument.Profiles,
            CategoryRewardPackages = rewardDocument.CategoryPackages,
            FavorRewards = revelationDocument.FavorRewards,
            WeeklyMilestones = revelationDocument.Milestones,
            Caches = cacheDocument.Caches,
            Economy = economyDocument.Economy
        };

        ThrowIfInvalid(_catalog, definitionProvider.GetAll());
    }

    public ProphecyBalanceCatalog GetCatalog() => _catalog;

    private static T Read<T>(string path, JsonSerializerOptions options) where T : new() =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), options) ?? new T();

    private static void ThrowIfInvalid(
        ProphecyBalanceCatalog catalog,
        IReadOnlyList<ProphecyDefinition> definitions)
    {
        ThrowIfDuplicates(catalog.Targets, x => $"{x.Scope}:{x.ObjectiveType}", "target profiles");
        ThrowIfDuplicates(catalog.RewardProfiles, x => x.Id, "reward profiles");
        ThrowIfDuplicates(catalog.CategoryRewardPackages,
            x => $"{x.Scope}:{x.Category}:{x.Difficulty?.ToString() ?? "Any"}",
            "category reward packages");
        ThrowIfDuplicates(catalog.FavorRewards, x => x.Scope.ToString(), "favor rewards");
        ThrowIfDuplicates(catalog.WeeklyMilestones, x => x.FavorRequired.ToString(), "weekly milestones");
        ThrowIfDuplicates(catalog.Caches, x => x.ItemId, "cache definitions");

        var invalidTargets = catalog.Targets
            .Where(x => string.IsNullOrWhiteSpace(x.ObjectiveType) ||
                        x.Values.Common <= 0 || x.Values.Uncommon <= 0 ||
                        x.Values.Rare <= 0 || x.Values.Epic <= 0)
            .Select(x => $"{x.Scope}:{x.ObjectiveType}")
            .ToList();
        ThrowIfAny(invalidTargets, "Prophecy target profiles require an objective type and positive values for every difficulty");

        var missingTargets = definitions
            .Where(definition => !catalog.Targets.Any(target =>
                target.Scope == definition.Scope &&
                string.Equals(target.ObjectiveType, definition.ObjectiveType, StringComparison.Ordinal)))
            .Select(x => x.Id)
            .ToList();
        ThrowIfAny(missingTargets, "Prophecy definitions reference missing target profiles");

        if (catalog.RewardScaling.CinderGrowthBasisPointsPerCharacterLevel < 0 ||
            catalog.RewardScaling.CinderGrowthCapBasisPoints < 0 ||
            catalog.RewardScaling.CinderRoundingIncrement <= 0)
        {
            throw new InvalidOperationException("Prophecy Cinder scaling values are invalid.");
        }

        var invalidProfiles = catalog.RewardProfiles
            .Where(x => string.IsNullOrWhiteSpace(x.Id) ||
                        x.CharacterExperience.NextLevelBasisPoints <= 0 ||
                        x.MinimumCinders < 0 ||
                        !IsValidReward(x.FlatReward) ||
                        HasScalableValues(x.FlatReward))
            .Select(x => string.IsNullOrWhiteSpace(x.Id) ? "<missing id>" : x.Id)
            .ToList();
        ThrowIfAny(invalidProfiles,
            "Prophecy reward profiles require an id, positive XP scaling, non-negative Cinders, and flat-only rewards");

        var missingProfiles = definitions
            .Where(definition => !catalog.RewardProfiles.Any(profile =>
                string.Equals(profile.Id, definition.RewardProfileId, StringComparison.OrdinalIgnoreCase)))
            .Select(x => $"{x.Id}:{x.RewardProfileId}")
            .ToList();
        ThrowIfAny(missingProfiles, "Prophecy definitions reference missing reward profiles");

        var mismatchedScopes = definitions
            .Select(definition => new
            {
                Definition = definition,
                Profile = catalog.RewardProfiles.FirstOrDefault(profile =>
                    string.Equals(profile.Id, definition.RewardProfileId, StringComparison.OrdinalIgnoreCase))
            })
            .Where(x => x.Profile is not null &&
                        (x.Profile.Scope != x.Definition.Scope || x.Profile.Difficulty != x.Definition.Difficulty))
            .Select(x => $"{x.Definition.Id}:{x.Definition.RewardProfileId}")
            .ToList();
        ThrowIfAny(mismatchedScopes, "Prophecy definitions and reward profiles must use the same scope and difficulty");

        var invalidCategoryPackages = catalog.CategoryRewardPackages
            .Where(package => !IsValidReward(package.Reward) ||
                              HasScalableValues(package.Reward) ||
                              HasInvalidLevelBands(package.LevelScaledItems))
            .Select(x => $"{x.Scope}:{x.Category}:{x.Difficulty?.ToString() ?? "Any"}")
            .ToList();
        ThrowIfAny(invalidCategoryPackages,
            "Prophecy category packages require flat-only rewards and valid, non-overlapping item level bands");

        var favorByScope = catalog.FavorRewards.ToDictionary(x => x.Scope, x => x.Amount);
        var missingFavorScopes = Enum.GetValues<ProphecyScope>()
            .Where(scope => !favorByScope.TryGetValue(scope, out var amount) || amount <= 0)
            .Select(x => x.ToString())
            .ToList();
        ThrowIfAny(missingFavorScopes, "Prophecy favor rewards require a positive value for every scope");

        var thresholds = catalog.WeeklyMilestones.Select(x => x.FavorRequired).Order().ToArray();
        if (!thresholds.SequenceEqual(PersistedMilestoneThresholds))
        {
            throw new InvalidOperationException(
                "Weekly Revelation milestone thresholds must be 3, 5, and 7 because those claim states are persisted explicitly.");
        }

        var invalidMilestones = catalog.WeeklyMilestones
            .Where(x => string.IsNullOrWhiteSpace(x.Title) || !IsValidReward(x.Reward))
            .Select(x => x.FavorRequired.ToString())
            .ToList();
        ThrowIfAny(invalidMilestones, "Weekly Revelation milestones require a title and non-negative rewards");

        var invalidCaches = catalog.Caches
            .Where(x => string.IsNullOrWhiteSpace(x.ItemId) ||
                        string.IsNullOrWhiteSpace(x.Title) ||
                        string.IsNullOrWhiteSpace(x.Description) ||
                        x.Rolls <= 0 ||
                        x.PreviewRewards.Count == 0 ||
                        x.PreviewRewards.Any(string.IsNullOrWhiteSpace) ||
                        x.Rewards.Count == 0 ||
                        x.Rewards.Any(reward => reward.Weight <= 0 || !IsValidReward(reward.Reward)))
            .Select(x => string.IsNullOrWhiteSpace(x.ItemId) ? "<missing id>" : x.ItemId)
            .ToList();
        ThrowIfAny(invalidCaches, "Prophecy caches require metadata, previews, positive rolls, and valid weighted rewards");

        var cacheIds = catalog.Caches.Select(x => x.ItemId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingCacheReferences = catalog.RewardProfiles.Select(x => (Owner: x.Id, CacheItemId: x.FlatReward.CacheItemId))
            .Concat(catalog.CategoryRewardPackages.Select(x =>
                (Owner: $"{x.Scope}:{x.Category}:{x.Difficulty?.ToString() ?? "Any"}", CacheItemId: x.Reward.CacheItemId)))
            .Concat(catalog.WeeklyMilestones.Select(x => (Owner: $"milestone:{x.FavorRequired}", CacheItemId: x.Reward.CacheItemId)))
            .Concat(catalog.Caches.SelectMany(cache => cache.Rewards.Select(x => (Owner: $"cache:{cache.ItemId}", CacheItemId: x.Reward.CacheItemId))))
            .Where(x => !string.IsNullOrWhiteSpace(x.CacheItemId) && !cacheIds.Contains(x.CacheItemId))
            .Select(x => $"{x.Owner}:{x.CacheItemId}")
            .ToList();
        ThrowIfAny(missingCacheReferences, "Prophecy rewards reference missing cache definitions");

        var economy = catalog.Economy;
        if (economy.DailyRerollLimit < 1 ||
            economy.PaidRerollCosts.Count != economy.DailyRerollLimit - 1 ||
            economy.PaidRerollCosts.Any(x => x <= 0))
        {
            throw new InvalidOperationException("Prophecy economy settings contain invalid limits, prices, or conversion values.");
        }

    }

    private static bool IsValidReward(ProphecyRewardSnapshot reward) =>
        reward.Cinders >= 0 &&
        reward.CharacterExperience >= 0 &&
        reward.EssenceExperience >= 0 &&
        reward.Soulstones >= 0 &&
        reward.SigilFragments >= 0 &&
        reward.PropheticFavor >= 0 &&
        reward.FateEcho >= 0 &&
        reward.Items.All(x => !string.IsNullOrWhiteSpace(x.ItemId) && x.Quantity > 0);

    private static bool HasScalableValues(ProphecyRewardSnapshot reward) =>
        reward.Cinders != 0 ||
        reward.CharacterExperience != 0 ||
        reward.EssenceExperience != 0 ||
        reward.PropheticFavor != 0;

    private static bool HasInvalidLevelBands(IReadOnlyList<ProphecyLevelScaledItemReward> bands)
    {
        if (bands.Any(x => x.MinLevel < 1 ||
                           x.MaxLevel < x.MinLevel ||
                           string.IsNullOrWhiteSpace(x.ItemId) ||
                           x.Quantity <= 0))
        {
            return true;
        }

        return bands
            .GroupBy(x => x.ItemId, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Any(candidate => group.Any(other =>
                !ReferenceEquals(candidate, other) &&
                candidate.MinLevel <= (other.MaxLevel ?? int.MaxValue) &&
                other.MinLevel <= (candidate.MaxLevel ?? int.MaxValue))));
    }

    private static void ThrowIfDuplicates<T>(
        IEnumerable<T> values,
        Func<T, string> keySelector,
        string label)
    {
        var duplicates = values
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        ThrowIfAny(duplicates, $"Duplicate prophecy {label}");
    }

    private static void ThrowIfAny(IReadOnlyCollection<string> values, string message)
    {
        if (values.Count > 0)
        {
            throw new InvalidOperationException($"{message}: {string.Join(", ", values)}");
        }
    }

    private sealed class TargetDocument
    {
        public List<ProphecyTargetProfile> Targets { get; set; } = [];
    }

    private sealed class RewardDocument
    {
        public ProphecyRewardScalingSettings Scaling { get; set; } = new();
        public List<ProphecyRewardProfile> Profiles { get; set; } = [];
        public List<ProphecyCategoryRewardPackage> CategoryPackages { get; set; } = [];
    }

    private sealed class WeeklyRevelationDocument
    {
        public List<ProphecyFavorReward> FavorRewards { get; set; } = [];
        public List<ProphecyWeeklyMilestoneDefinition> Milestones { get; set; } = [];
    }

    private sealed class CacheDocument
    {
        public List<ProphecyCacheDefinition> Caches { get; set; } = [];
    }

    private sealed class EconomyDocument
    {
        public ProphecyEconomySettings Economy { get; set; } = new();
    }
}
