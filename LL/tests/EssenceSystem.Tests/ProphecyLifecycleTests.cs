using System.Text.Json;
using Application.Interfaces.Services.LL.Prophecies;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
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
    public async Task RerollAsync_replaces_all_three_offers_and_consumes_one_daily_use()
    {
        var fixture = CreateFixture();
        var overview = await fixture.Service.GetOverviewAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            Now,
            CancellationToken.None);
        var originalDefinitionIds = overview.DailyProphecies.ToDictionary(
            x => x.Id,
            x => x.ProphecyDefinitionId);

        var result = await fixture.Service.RerollAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            Now.AddMinutes(1),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.All(overview.DailyProphecies, prophecy =>
        {
            Assert.NotEqual(originalDefinitionIds[prophecy.Id], prophecy.ProphecyDefinitionId);
            Assert.Equal(originalDefinitionIds[prophecy.Id], prophecy.RerolledFromDefinitionId);
        });
        Assert.Equal(3, overview.DailyProphecies.Select(x => x.ProphecyDefinitionId).Distinct().Count());
        Assert.Equal(0, result.Value?.DailyRerollsRemaining);
        Assert.Equal(1, result.Value?.DailyRerollsUsed);
        Assert.Equal(1, fixture.Repository.RerollConsumeCount);
    }

    [Fact]
    public async Task RerollAsync_charges_escalating_Fate_Echo_after_the_free_use()
    {
        var fixture = CreateFixture();
        var overview = await fixture.Service.GetOverviewAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            Now,
            CancellationToken.None);
        var free = await fixture.Service.RerollAsync(
            fixture.PlayerId, fixture.CharacterId, Now.AddMinutes(1), CancellationToken.None);
        var second = await fixture.Service.RerollAsync(
            fixture.PlayerId, fixture.CharacterId, Now.AddMinutes(2), CancellationToken.None);
        var third = await fixture.Service.RerollAsync(
            fixture.PlayerId, fixture.CharacterId, Now.AddMinutes(3), CancellationToken.None);
        var overLimit = await fixture.Service.RerollAsync(
            fixture.PlayerId, fixture.CharacterId, Now.AddMinutes(4), CancellationToken.None);

        Assert.True(free.Succeeded);
        Assert.True(second.Succeeded);
        Assert.True(third.Succeeded);
        Assert.False(overLimit.Succeeded);
        Assert.Equal(0, fixture.Character.FateEcho);
        Assert.Equal(3, third.Value?.DailyRerollsUsed);
        Assert.Null(third.Value?.NextDailyRerollCost);
        Assert.Equal(120, fixture.Repository.RerollStates.Single().FateEchoSpent);
    }

    [Fact]
    public async Task RerollAsync_does_not_change_offer_or_balance_when_Fate_Echo_is_insufficient()
    {
        var fixture = CreateFixture();
        fixture.Character.FateEcho = 39;
        var overview = await fixture.Service.GetOverviewAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            Now,
            CancellationToken.None);
        var free = await fixture.Service.RerollAsync(
            fixture.PlayerId, fixture.CharacterId, Now.AddMinutes(1), CancellationToken.None);
        var definitionsAfterFreeUse = overview.DailyProphecies.ToDictionary(
            x => x.Id,
            x => x.ProphecyDefinitionId);

        var paid = await fixture.Service.RerollAsync(
            fixture.PlayerId, fixture.CharacterId, Now.AddMinutes(2), CancellationToken.None);

        Assert.True(free.Succeeded);
        Assert.False(paid.Succeeded);
        Assert.Equal(39, fixture.Character.FateEcho);
        Assert.All(overview.DailyProphecies, prophecy =>
            Assert.Equal(definitionsAfterFreeUse[prophecy.Id], prophecy.ProphecyDefinitionId));
        Assert.Equal(1, fixture.Repository.RerollStates.Single().RerollsUsed);
    }

    [Fact]
    public async Task RerollAsync_does_not_consume_or_partially_replace_when_a_complete_set_is_unavailable()
    {
        var fixture = CreateFixture(
        [
            Definition("daily.steady.only", ProphecyScope.Daily, ProphecySlotType.Steady, ProphecyCategory.Combat),
            Definition("daily.focused.only", ProphecyScope.Daily, ProphecySlotType.Focused, ProphecyCategory.Essence),
            Definition("daily.ominous.only", ProphecyScope.Daily, ProphecySlotType.Ominous, ProphecyCategory.Gathering),
            Definition("weekly.greater", ProphecyScope.Weekly, ProphecySlotType.Greater, ProphecyCategory.Combat)
        ]);
        var overview = await fixture.Service.GetOverviewAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            Now,
            CancellationToken.None);
        var originalDefinitionIds = overview.DailyProphecies.ToDictionary(
            x => x.Id,
            x => x.ProphecyDefinitionId);

        var result = await fixture.Service.RerollAsync(
            fixture.PlayerId,
            fixture.CharacterId,
            Now.AddMinutes(1),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(120, fixture.Character.FateEcho);
        Assert.Equal(0, fixture.Repository.RerollConsumeCount);
        Assert.Equal(0, fixture.Repository.RerollStates.Single().RerollsUsed);
        Assert.All(overview.DailyProphecies, prophecy =>
            Assert.Equal(originalDefinitionIds[prophecy.Id], prophecy.ProphecyDefinitionId));
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

    private static Fixture CreateFixture(IReadOnlyList<ProphecyDefinition>? definitions = null)
    {
        var playerId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        definitions ??= CreateDefinitions();
        var repository = new LifecycleRepository(definitions);
        var character = new Character
        {
            Id = characterId,
            UserId = playerId,
            Name = "Oracle",
            FateEcho = 120,
        };
        var balanceProvider = new BalanceProvider();
        var service = new ProphecyService(
            new DefinitionProvider(definitions),
            balanceProvider,
            new ProphecyRewardResolver(balanceProvider),
            repository,
            new CharacterService(character),
            new EntityService(),
            null!,
            null!,
            null!,
            null!);

        return new Fixture(playerId, characterId, character, repository, service);
    }

    private static IReadOnlyList<ProphecyDefinition> CreateDefinitions() =>
    [
        Definition("daily.steady.combat", ProphecyScope.Daily, ProphecySlotType.Steady, ProphecyCategory.Combat),
        Definition("daily.steady.dungeon", ProphecyScope.Daily, ProphecySlotType.Steady, ProphecyCategory.Dungeon),
        Definition("daily.steady.survival", ProphecyScope.Daily, ProphecySlotType.Steady, ProphecyCategory.Survival),
        Definition("daily.steady.treasure", ProphecyScope.Daily, ProphecySlotType.Steady, ProphecyCategory.Treasure),
        Definition("daily.focused", ProphecyScope.Daily, ProphecySlotType.Focused, ProphecyCategory.Essence),
        Definition("daily.focused.dungeon", ProphecyScope.Daily, ProphecySlotType.Focused, ProphecyCategory.Dungeon),
        Definition("daily.ominous", ProphecyScope.Daily, ProphecySlotType.Ominous, ProphecyCategory.Gathering),
        Definition("daily.ominous.survival", ProphecyScope.Daily, ProphecySlotType.Ominous, ProphecyCategory.Survival),
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
        Character Character,
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
                    Difficulty = ProphecyDifficulty.Common,
                    CharacterExperience = new ProphecyScaledAmount { Minimum = 1, NextLevelBasisPoints = 1 },
                    MinimumCinders = 10
                },
                new ProphecyRewardProfile
                {
                    Id = "Weekly.Test",
                    Scope = ProphecyScope.Weekly,
                    Difficulty = ProphecyDifficulty.Common,
                    CharacterExperience = new ProphecyScaledAmount { Minimum = 1, NextLevelBasisPoints = 1 },
                    MinimumCinders = 20
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
        public List<DailyProphecyRerollState> RerollStates { get; } = [];
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

        public Task<DailyProphecyRerollState?> GetDailyRerollStateAsync(
            Guid playerId,
            Guid characterId,
            DateTimeOffset periodStart,
            CancellationToken cancellationToken) =>
            Task.FromResult(RerollStates.FirstOrDefault(x =>
                x.PlayerId == playerId && x.CharacterId == characterId && x.PeriodStart == periodStart));

        public Task AddDailyRerollStateAsync(
            DailyProphecyRerollState state,
            CancellationToken cancellationToken)
        {
            RerollStates.Add(state);
            return Task.CompletedTask;
        }

        public Task<bool> TrySpendFateEchoAsync(
            Guid characterId,
            long amount,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);

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

    private sealed class EntityService : IEntityService
    {
        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void UpdateEntities(List<Entity> playerCharacters)
        {
        }
    }

    private sealed class CharacterService(Character character) : ICharacterService
    {
        public Task<Character?> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<Character?>(character.Id == characterId ? character : null);

        public Task<int> GetCombatRatingAsync(Guid characterId, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<Character> CreateCharacterAsync(Guid userId, string username, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetMyCharacterAsync(Guid currentUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetMyCharacterOverviewAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterOverviewByNameAsync(string characterName, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> UpdateCharacterNameAsync(Guid userId, string username, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsCharacterNameTakenAsync(string name, Guid? excludedCharacterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetBaseCharacterByIdAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> GetCharacterIdByNameAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
