using System.Text.Json;
using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Prophecies;
using Services.LL.Prophecies;

namespace EssenceSystem.Tests;

public sealed class ProphecyLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetOverviewAsync_generates_stable_daily_and_weekly_instances_with_snapshots()
    {
        var fixture = CreateFixture();

        var first = await fixture.Service.GetOverviewAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            Now,
            CancellationToken.None);
        var second = await fixture.Service.GetOverviewAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            Now.AddHours(1),
            CancellationToken.None);

        Assert.Equal(3, first.DailyProphecies.Count);
        Assert.Equal(first.DailyProphecies.Select(x => x.Id), second.DailyProphecies.Select(x => x.Id));
        Assert.Equal(4, fixture.Repository.Instances.Count);
        Assert.All(first.DailyProphecies, prophecy =>
        {
            Assert.Equal(ProphecyStatus.Offered, prophecy.Status);
            Assert.Equal(new DateTimeOffset(2026, 7, 14, 0, 0, 0, TimeSpan.Zero), prophecy.PeriodStart);
            Assert.Equal(new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero), prophecy.PeriodEnd);
            Assert.Equal(5, prophecy.TargetValue);
            Assert.Equal(prophecy.ProphecyDefinition?.ObjectiveParameterJson, prophecy.ObjectiveParameterSnapshotJson);
            Assert.Equal(10, ReadReward(prophecy).Cinders);
        });

        Assert.Equal(first.GreaterProphecy.Id, second.GreaterProphecy.Id);
        Assert.Equal(ProphecyStatus.Accepted, first.GreaterProphecy.Status);
        Assert.Equal(new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero), first.GreaterProphecy.PeriodStart);
        Assert.Equal(new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.Zero), first.GreaterProphecy.PeriodEnd);
        Assert.Equal(20, first.GreaterProphecy.TargetValue);
        Assert.Equal(20, ReadReward(first.GreaterProphecy).Cinders);
        Assert.Equal(first.GreaterProphecy.PeriodStart, first.WeeklyRevelation.PeriodStart);
    }

    [Fact]
    public async Task AcceptAsync_accepts_one_daily_and_declines_the_other_offers()
    {
        var fixture = CreateFixture();
        var overview = await fixture.Service.GetOverviewAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            Now,
            CancellationToken.None);
        var selected = overview.DailyProphecies.Single(x => x.SlotType == ProphecySlotType.Focused);

        var result = await fixture.Service.AcceptAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            selected.Id,
            Now.AddMinutes(1),
            CancellationToken.None);
        var secondChoice = overview.DailyProphecies.First(x => x.Id != selected.Id);
        var replay = await fixture.Service.AcceptAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            secondChoice.Id,
            Now.AddMinutes(2),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(selected.Id, result.Value?.ActiveDailyProphecy?.Id);
        Assert.Equal(ProphecyStatus.Accepted, selected.Status);
        Assert.Equal(Now.AddMinutes(1), selected.AcceptedAt);
        Assert.All(overview.DailyProphecies.Where(x => x.Id != selected.Id), x => Assert.Equal(ProphecyStatus.Declined, x.Status));
        Assert.False(replay.Succeeded);
    }

    [Fact]
    public async Task RerollAsync_replaces_an_offer_and_consumes_the_daily_use_once()
    {
        var fixture = CreateFixture();
        var overview = await fixture.Service.GetOverviewAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            Now,
            CancellationToken.None);
        var selected = overview.DailyProphecies.Single(x => x.SlotType == ProphecySlotType.Steady);
        var originalDefinitionId = selected.ProphecyDefinitionId;

        var result = await fixture.Service.RerollAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            selected.Id,
            Now.AddMinutes(1),
            CancellationToken.None);
        var otherOffer = overview.DailyProphecies.Single(x => x.SlotType == ProphecySlotType.Focused);
        var replay = await fixture.Service.RerollAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            otherOffer.Id,
            Now.AddMinutes(2),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotEqual(originalDefinitionId, selected.ProphecyDefinitionId);
        Assert.Equal(originalDefinitionId, selected.RerolledFromDefinitionId);
        Assert.Equal(0, result.Value?.DailyRerollsRemaining);
        Assert.Equal(1, fixture.Repository.RerollConsumeCount);
        Assert.False(replay.Succeeded);
        Assert.Equal(1, fixture.Repository.RerollConsumeCount);
    }

    [Fact]
    public async Task GetOverviewAsync_creates_new_daily_instances_after_utc_rollover()
    {
        var fixture = CreateFixture();
        var first = await fixture.Service.GetOverviewAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            Now,
            CancellationToken.None);
        var nextDay = await fixture.Service.GetOverviewAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            Now.AddDays(1),
            CancellationToken.None);

        Assert.Empty(first.DailyProphecies.Select(x => x.Id).Intersect(nextDay.DailyProphecies.Select(x => x.Id)));
        Assert.Equal(first.GreaterProphecy.Id, nextDay.GreaterProphecy.Id);
        Assert.Equal(new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero), nextDay.DailyProphecies[0].PeriodStart);
        Assert.Equal(7, fixture.Repository.Instances.Count);
    }

    private static Fixture CreateFixture()
    {
        var playerId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new LifecycleRepository(definitions);
        var service = new ProphecyService(
            new DefinitionProvider(definitions),
            new BalanceProvider(),
            repository,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        return new Fixture(playerId, characterId, repository, service);
    }

    private static IReadOnlyList<ProphecyDefinition> CreateDefinitions() =>
    [
        Definition("daily.steady.combat", ProphecyScope.Daily, ProphecySlotType.Steady, ProphecyCategory.Combat),
        Definition("daily.steady.dungeon", ProphecyScope.Daily, ProphecySlotType.Steady, ProphecyCategory.Dungeon),
        Definition("daily.focused", ProphecyScope.Daily, ProphecySlotType.Focused, ProphecyCategory.Essence),
        Definition("daily.ominous", ProphecyScope.Daily, ProphecySlotType.Ominous, ProphecyCategory.Gathering),
        Definition("weekly.greater", ProphecyScope.Weekly, ProphecySlotType.Greater, ProphecyCategory.Combat)
    ];

    private static ProphecyDefinition Definition(
        string id,
        ProphecyScope scope,
        ProphecySlotType slot,
        ProphecyCategory category) =>
        new()
        {
            Id = id,
            Title = id,
            FlavorText = "Test flavor",
            ObjectiveText = "Defeat {target} creatures.",
            Scope = scope,
            Category = category,
            Difficulty = ProphecyDifficulty.Common,
            ObjectiveType = ProphecyObjectiveType.KillCreatures,
            ObjectiveParameterJson = $"{{\"definition\":\"{id}\"}}",
            RewardProfileId = scope == ProphecyScope.Daily ? "Daily.Test" : "Weekly.Test",
            Weight = 100,
            AllowedSlots = [slot.ToString()]
        };

    private static ProphecyRewardSnapshot ReadReward(PlayerProphecyInstance prophecy) =>
        JsonSerializer.Deserialize<ProphecyRewardSnapshot>(prophecy.RewardSnapshotJson, JsonOptions)!;

    private sealed record Fixture(
        Guid PlayerId,
        Guid CharacterId,
        LifecycleRepository Repository,
        ProphecyService Service);

    private sealed class DefinitionProvider(IReadOnlyList<ProphecyDefinition> definitions) : IProphecyDefinitionProvider
    {
        public IReadOnlyList<ProphecyDefinition> GetAll() => definitions;
    }

    private sealed class BalanceProvider : IProphecyBalanceProvider
    {
        public ProphecyBalanceCatalog GetCatalog() => new()
        {
            Targets =
            [
                Target(ProphecyScope.Daily, 5),
                Target(ProphecyScope.Weekly, 20)
            ],
            RewardProfiles =
            [
                new ProphecyRewardProfile
                {
                    Id = "Daily.Test",
                    Scope = ProphecyScope.Daily,
                    Reward = new ProphecyRewardSnapshot { Cinders = 10 }
                },
                new ProphecyRewardProfile
                {
                    Id = "Weekly.Test",
                    Scope = ProphecyScope.Weekly,
                    Reward = new ProphecyRewardSnapshot { Cinders = 20 }
                }
            ],
            FavorRewards =
            [
                new ProphecyFavorReward { Scope = ProphecyScope.Daily, Amount = 1 },
                new ProphecyFavorReward { Scope = ProphecyScope.Weekly, Amount = 2 }
            ],
            WeeklyMilestones =
            [
                new ProphecyWeeklyMilestoneDefinition { FavorRequired = 3, Title = "Three" },
                new ProphecyWeeklyMilestoneDefinition { FavorRequired = 5, Title = "Five" },
                new ProphecyWeeklyMilestoneDefinition { FavorRequired = 7, Title = "Seven" }
            ]
        };

        private static ProphecyTargetProfile Target(ProphecyScope scope, int value) =>
            new()
            {
                Scope = scope,
                ObjectiveType = ProphecyObjectiveType.KillCreatures,
                Values = new ProphecyDifficultyTargets
                {
                    Common = value,
                    Uncommon = value,
                    Rare = value,
                    Epic = value
                }
            };
    }

    private sealed class LifecycleRepository(IReadOnlyList<ProphecyDefinition> definitions) : IProphecyRepository
    {
        public List<PlayerProphecyInstance> Instances { get; } = [];
        public List<WeeklyRevelationProgress> WeeklyProgress { get; } = [];
        public int RerollConsumeCount { get; private set; }

        public Task<IReadOnlyList<ProphecyDefinition>> GetEnabledDefinitionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(definitions);

        public Task<IReadOnlyList<ProphecyDefinition>> SyncDefinitionsAsync(
            IReadOnlyCollection<ProphecyDefinition> authoredDefinitions,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ProphecyDefinition>>(authoredDefinitions.ToList());

        public Task<IReadOnlyList<PlayerProphecyInstance>> GetInstancesForPeriodAsync(
            Guid playerId,
            Guid characterId,
            ProphecyScope scope,
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlayerProphecyInstance>>(Instances
                .Where(x =>
                    x.PlayerId == playerId &&
                    x.CharacterId == characterId &&
                    x.Scope == scope &&
                    x.PeriodStart == periodStart &&
                    x.PeriodEnd == periodEnd)
                .OrderBy(x => x.SlotType)
                .ToList());

        public Task<PlayerProphecyInstance?> GetInstanceAsync(
            Guid instanceId,
            Guid playerId,
            Guid characterId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Instances.FirstOrDefault(x =>
                x.Id == instanceId &&
                x.PlayerId == playerId &&
                x.CharacterId == characterId));

        public Task AddInstancesAsync(
            IReadOnlyCollection<PlayerProphecyInstance> instances,
            CancellationToken cancellationToken)
        {
            foreach (var instance in instances)
            {
                instance.ProphecyDefinition ??= definitions.First(x => x.Id == instance.ProphecyDefinitionId);
                Instances.Add(instance);
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PlayerProphecyInstance>> GetAcceptedInstancesForProgressAsync(
            Guid characterId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlayerProphecyInstance>>(Instances
                .Where(x => x.CharacterId == characterId && x.Status == ProphecyStatus.Accepted)
                .ToList());

        public Task<IReadOnlyList<PlayerProphecyInstance>> GetAcceptedInstancesForProgressWindowAsync(
            Guid characterId,
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken cancellationToken) =>
            GetAcceptedInstancesForProgressAsync(characterId, to, cancellationToken);

        public Task<IReadOnlyList<PlayerProphecyInstance>> GetRecentInstancesAsync(
            Guid playerId,
            Guid characterId,
            DateTimeOffset since,
            int limit,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlayerProphecyInstance>>(Instances
                .Where(x =>
                    x.PlayerId == playerId &&
                    x.CharacterId == characterId &&
                    x.GeneratedAt >= since &&
                    x.Status is not (ProphecyStatus.Offered or ProphecyStatus.Accepted))
                .OrderByDescending(x => x.GeneratedAt)
                .Take(limit)
                .ToList());

        public Task<IReadOnlySet<string>> GetRecentDefinitionIdsAsync(
            Guid playerId,
            Guid characterId,
            ProphecyScope scope,
            DateTimeOffset since,
            DateTimeOffset before,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(Instances
                .Where(x =>
                    x.PlayerId == playerId &&
                    x.CharacterId == characterId &&
                    x.Scope == scope &&
                    x.GeneratedAt >= since &&
                    x.GeneratedAt < before)
                .SelectMany(x => new[] { x.ProphecyDefinitionId, x.RerolledFromDefinitionId })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase));

        public Task<bool> TryConsumeDailyRerollAsync(
            Guid playerId,
            Guid characterId,
            DateTimeOffset periodStart,
            DateTimeOffset usedAt,
            CancellationToken cancellationToken)
        {
            var anchor = Instances.FirstOrDefault(x =>
                x.PlayerId == playerId &&
                x.CharacterId == characterId &&
                x.Scope == ProphecyScope.Daily &&
                x.PeriodStart == periodStart &&
                x.SlotType == ProphecySlotType.Steady);

            if (anchor is null || anchor.DailyRerollUsedAt.HasValue)
            {
                return Task.FromResult(false);
            }

            anchor.DailyRerollUsedAt = usedAt;
            RerollConsumeCount++;
            return Task.FromResult(true);
        }

        public Task<WeeklyRevelationProgress?> GetWeeklyProgressAsync(
            Guid playerId,
            Guid characterId,
            DateTimeOffset periodStart,
            CancellationToken cancellationToken) =>
            Task.FromResult(WeeklyProgress.FirstOrDefault(x =>
                x.PlayerId == playerId &&
                x.CharacterId == characterId &&
                x.PeriodStart == periodStart));

        public Task AddWeeklyProgressAsync(
            WeeklyRevelationProgress progress,
            CancellationToken cancellationToken)
        {
            WeeklyProgress.Add(progress);
            return Task.CompletedTask;
        }
    }
}
