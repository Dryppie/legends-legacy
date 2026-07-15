using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Prophecies;

namespace Services.LL.Prophecies;

public sealed class ProphecyRewardResolver : IProphecyRewardResolver
{
    private readonly ProphecyBalanceCatalog _balance;

    public ProphecyRewardResolver(IProphecyBalanceProvider balanceProvider)
    {
        _balance = balanceProvider.GetCatalog();
    }

    public ProphecyRewardSnapshot Resolve(ProphecyDefinition definition, ProphecyRewardContext context)
    {
        var profile = _balance.RewardProfiles.First(x =>
            x.Id.Equals(definition.RewardProfileId, StringComparison.OrdinalIgnoreCase));

        var characterExperience = context.ExperienceRequiredForNextLevel <= 0
            ? 0
            : Math.Max(1, checked((long)Math.Round(
                (decimal)context.ExperienceRequiredForNextLevel * profile.CharacterExperience.NextLevelBasisPoints / 10_000m,
                MidpointRounding.AwayFromZero)));
        var uncappedGrowthBasisPoints = (long)Math.Max(0, context.CharacterLevel - 1) *
                                        _balance.RewardScaling.CinderGrowthBasisPointsPerCharacterLevel;
        var growthBasisPoints = Math.Min(
            _balance.RewardScaling.CinderGrowthCapBasisPoints,
            uncappedGrowthBasisPoints);
        var cinders = Math.Max(profile.MinimumCinders, RoundToIncrement(
            profile.MinimumCinders * (10_000d + growthBasisPoints) / 10_000d,
            _balance.RewardScaling.CinderRoundingIncrement));

        var reward = Clone(profile.FlatReward);
        reward.CharacterExperience = characterExperience;
        reward.Cinders = cinders;
        reward.PropheticFavor = _balance.FavorRewards.First(x => x.Scope == definition.Scope).Amount;

        foreach (var package in _balance.CategoryRewardPackages.Where(x =>
                     x.Scope == definition.Scope &&
                     x.Category == definition.Category &&
                     (x.Difficulty is null || x.Difficulty == definition.Difficulty)))
        {
            Add(reward, package.Reward);
            foreach (var item in package.LevelScaledItems.Where(x =>
                         context.CharacterLevel >= x.MinLevel &&
                         (x.MaxLevel is null || context.CharacterLevel <= x.MaxLevel)))
            {
                AddItem(reward.Items, item.ItemId, item.Quantity);
            }
        }

        return reward;
    }

    private static long RoundToIncrement(double value, int increment) =>
        (long)Math.Round(value / increment, MidpointRounding.AwayFromZero) * increment;

    private static ProphecyRewardSnapshot Clone(ProphecyRewardSnapshot reward) =>
        new()
        {
            Cinders = reward.Cinders,
            CharacterExperience = reward.CharacterExperience,
            EssenceExperience = reward.EssenceExperience,
            Soulstones = reward.Soulstones,
            SigilFragments = reward.SigilFragments,
            PropheticFavor = reward.PropheticFavor,
            FateEcho = reward.FateEcho,
            CacheItemId = reward.CacheItemId,
            Items = reward.Items.Select(x => new RewardItemSnapshot
            {
                ItemId = x.ItemId,
                Quantity = x.Quantity
            }).ToList()
        };

    private static void Add(ProphecyRewardSnapshot target, ProphecyRewardSnapshot addition)
    {
        target.Cinders += addition.Cinders;
        target.CharacterExperience += addition.CharacterExperience;
        target.EssenceExperience += addition.EssenceExperience;
        target.Soulstones += addition.Soulstones;
        target.SigilFragments += addition.SigilFragments;
        target.PropheticFavor += addition.PropheticFavor;
        target.FateEcho += addition.FateEcho;
        target.CacheItemId ??= addition.CacheItemId;
        foreach (var item in addition.Items)
        {
            AddItem(target.Items, item.ItemId, item.Quantity);
        }
    }

    private static void AddItem(List<RewardItemSnapshot> items, string itemId, int quantity)
    {
        var existing = items.FirstOrDefault(x => x.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            items.Add(new RewardItemSnapshot { ItemId = itemId, Quantity = quantity });
            return;
        }

        existing.Quantity += quantity;
    }
}
