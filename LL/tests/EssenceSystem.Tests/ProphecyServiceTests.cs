using System.Text.Json;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Prophecies;
using Services.LL.Prophecies;

namespace EssenceSystem.Tests;

public sealed class ProphecyServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("Mining", "Mining")]
    [InlineData("Woodcutting", "woodcutting")]
    [InlineData("Fishing", " Fishing ")]
    public async Task TrackProgressAsync_counts_gathering_from_required_profession(
        string requiredProfession,
        string eventProfession)
    {
        var prophecy = CreateGatheringProphecy($"{{\"requiredProfession\":\"{requiredProfession}\"}}");
        var service = CreateService(prophecy);

        var updates = await service.TrackProgressAsync(
            new ProphecyProgressEvent(
                prophecy.CharacterId,
                Now,
                ProphecyProgressKind.ResourceGathered,
                Amount: 7,
                Profession: eventProfession),
            CancellationToken.None);

        var update = Assert.Single(updates);
        Assert.Equal(7, prophecy.CurrentValue);
        Assert.Equal(7, update.AmountGained);
    }

    [Theory]
    [InlineData("Mining", "Woodcutting")]
    [InlineData("Fishing", "Mining")]
    [InlineData("Woodcutting", null)]
    public async Task TrackProgressAsync_ignores_gathering_from_other_professions(
        string requiredProfession,
        string? eventProfession)
    {
        var prophecy = CreateGatheringProphecy($"{{\"requiredProfession\":\"{requiredProfession}\"}}");
        var service = CreateService(prophecy);

        var updates = await service.TrackProgressAsync(
            new ProphecyProgressEvent(
                prophecy.CharacterId,
                Now,
                ProphecyProgressKind.ResourceGathered,
                Amount: 7,
                Profession: eventProfession),
            CancellationToken.None);

        Assert.Empty(updates);
        Assert.Equal(0, prophecy.CurrentValue);
    }

    [Fact]
    public async Task TrackProgressAsync_counts_unrestricted_weekly_gathering()
    {
        var prophecy = CreateGatheringProphecy("{}");
        var service = CreateService(prophecy);

        var updates = await service.TrackProgressAsync(
            new ProphecyProgressEvent(
                prophecy.CharacterId,
                Now,
                ProphecyProgressKind.ResourceGathered,
                Amount: 11,
                Profession: "Mining"),
            CancellationToken.None);

        Assert.Single(updates);
        Assert.Equal(11, prophecy.CurrentValue);
    }

    [Fact]
    public async Task TrackProgressAsync_fails_closed_for_malformed_gathering_parameters()
    {
        var prophecy = CreateGatheringProphecy("not-json");
        var service = CreateService(prophecy);

        var updates = await service.TrackProgressAsync(
            new ProphecyProgressEvent(
                prophecy.CharacterId,
                Now,
                ProphecyProgressKind.ResourceGathered,
                Amount: 5,
                Profession: "Mining"),
            CancellationToken.None);

        Assert.Empty(updates);
        Assert.Equal(0, prophecy.CurrentValue);
    }

    [Theory]
    [InlineData(ProphecyObjectiveType.KillCreatures, ProphecyProgressKind.CreatureDefeated)]
    [InlineData(ProphecyObjectiveType.WinEncounters, ProphecyProgressKind.EncounterWon)]
    [InlineData(ProphecyObjectiveType.ClearDungeonRooms, ProphecyProgressKind.DungeonRoomCleared)]
    [InlineData(ProphecyObjectiveType.CompleteDungeons, ProphecyProgressKind.DungeonCompleted)]
    [InlineData(ProphecyObjectiveType.ResolveDungeonEvents, ProphecyProgressKind.DungeonEventResolved)]
    [InlineData(ProphecyObjectiveType.GainEssenceXp, ProphecyProgressKind.EssenceXpGained)]
    [InlineData(ProphecyObjectiveType.AbsorbEssence, ProphecyProgressKind.EssenceAbsorbed)]
    [InlineData(ProphecyObjectiveType.TemperItems, ProphecyProgressKind.ItemTempered)]
    [InlineData(ProphecyObjectiveType.SpendPotential, ProphecyProgressKind.PotentialSpent)]
    [InlineData(ProphecyObjectiveType.TreasureProgress, ProphecyProgressKind.TreasureProgress)]
    public async Task TrackProgressAsync_counts_each_direct_objective_matcher(
        string objectiveType,
        ProphecyProgressKind progressKind)
    {
        var prophecy = CreateProphecy(objectiveType);
        var service = CreateService(prophecy);

        var updates = await service.TrackProgressAsync(
            new ProphecyProgressEvent(prophecy.CharacterId, Now, progressKind, Amount: 3),
            CancellationToken.None);

        var update = Assert.Single(updates);
        Assert.Equal(3, prophecy.CurrentValue);
        Assert.Equal(3, update.AmountGained);
    }

    [Fact]
    public async Task TrackProgressAsync_enforces_minimum_enemy_count_and_fails_closed_for_malformed_parameters()
    {
        var prophecy = CreateProphecy(
            ProphecyObjectiveType.WinEncounters,
            "{\"minimumEnemyCount\":3}");
        var service = CreateService(prophecy);

        var tooSmall = await service.TrackProgressAsync(
            new ProphecyProgressEvent(
                prophecy.CharacterId,
                Now,
                ProphecyProgressKind.EncounterWon,
                EnemyCount: 2),
            CancellationToken.None);
        var qualifying = await service.TrackProgressAsync(
            new ProphecyProgressEvent(
                prophecy.CharacterId,
                Now,
                ProphecyProgressKind.EncounterWon,
                EnemyCount: 3),
            CancellationToken.None);

        Assert.Empty(tooSmall);
        Assert.Single(qualifying);
        Assert.Equal(1, prophecy.CurrentValue);

        var malformed = CreateProphecy(ProphecyObjectiveType.WinEncounters, "not-json");
        var malformedUpdates = await CreateService(malformed).TrackProgressAsync(
            new ProphecyProgressEvent(
                malformed.CharacterId,
                Now,
                ProphecyProgressKind.EncounterWon,
                EnemyCount: 3),
            CancellationToken.None);

        Assert.Empty(malformedUpdates);
        Assert.Equal(0, malformed.CurrentValue);
    }

    [Fact]
    public async Task TrackProgressAsync_counts_each_creature_type_once()
    {
        var prophecy = CreateProphecy(ProphecyObjectiveType.KillDifferentCreatureTypes);
        var service = CreateService(prophecy);

        var updates = await service.TrackProgressAsync(
        [
            new ProphecyProgressEvent(prophecy.CharacterId, Now, ProphecyProgressKind.CreatureDefeated, CreatureDefinitionId: "wolf"),
            new ProphecyProgressEvent(prophecy.CharacterId, Now, ProphecyProgressKind.CreatureDefeated, CreatureDefinitionId: "WOLF"),
            new ProphecyProgressEvent(prophecy.CharacterId, Now, ProphecyProgressKind.CreatureDefeated, CreatureDefinitionId: "bear")
        ], CancellationToken.None);

        var update = Assert.Single(updates);
        Assert.Equal(2, prophecy.CurrentValue);
        Assert.Equal(2, update.AmountGained);
        var progress = JsonSerializer.Deserialize<ProphecyProgressSnapshot>(prophecy.ProgressJson, JsonOptions);
        Assert.Equal(2, progress?.UniqueIds.Count);
    }

    [Fact]
    public async Task TrackProgressAsync_requires_a_defeat_before_counting_recovery_wins()
    {
        var prophecy = CreateProphecy(ProphecyObjectiveType.MeaningfulDefeatThenWins);
        var service = CreateService(prophecy);

        var beforeDefeat = await service.TrackProgressAsync(
            new ProphecyProgressEvent(prophecy.CharacterId, Now, ProphecyProgressKind.EncounterWon),
            CancellationToken.None);
        var recovery = await service.TrackProgressAsync(
        [
            new ProphecyProgressEvent(prophecy.CharacterId, Now.AddMinutes(1), ProphecyProgressKind.EncounterLost),
            new ProphecyProgressEvent(prophecy.CharacterId, Now.AddMinutes(2), ProphecyProgressKind.EncounterWon),
            new ProphecyProgressEvent(prophecy.CharacterId, Now.AddMinutes(3), ProphecyProgressKind.EncounterWon)
        ], CancellationToken.None);

        Assert.Empty(beforeDefeat);
        Assert.Single(recovery);
        Assert.Equal(2, prophecy.CurrentValue);
        Assert.True(JsonSerializer.Deserialize<ProphecyProgressSnapshot>(prophecy.ProgressJson, JsonOptions)?.HasMeaningfulDefeat);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(3600, false)]
    public async Task TrackProgressAsync_respects_acceptance_and_half_open_period_boundaries(
        int secondsFromPeriodStart,
        bool shouldCount)
    {
        var prophecy = CreateProphecy(ProphecyObjectiveType.KillCreatures);
        prophecy.AcceptedAt = Now;
        prophecy.PeriodStart = Now;
        prophecy.PeriodEnd = Now.AddHours(1);
        var service = CreateService(prophecy);

        var updates = await service.TrackProgressAsync(
            new ProphecyProgressEvent(
                prophecy.CharacterId,
                Now.AddSeconds(secondsFromPeriodStart),
                ProphecyProgressKind.CreatureDefeated),
            CancellationToken.None);

        Assert.Equal(shouldCount, updates.Count == 1);
        Assert.Equal(shouldCount ? 1 : 0, prophecy.CurrentValue);
    }

    [Fact]
    public async Task TrackProgressAsync_rejects_batches_for_multiple_characters()
    {
        var prophecy = CreateProphecy(ProphecyObjectiveType.KillCreatures);
        var service = CreateService(prophecy);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.TrackProgressAsync(
        [
            new ProphecyProgressEvent(prophecy.CharacterId, Now, ProphecyProgressKind.CreatureDefeated),
            new ProphecyProgressEvent(Guid.NewGuid(), Now, ProphecyProgressKind.CreatureDefeated)
        ], CancellationToken.None));
    }

    [Fact]
    public async Task TrackProgressAsync_aggregates_a_batch_into_one_update_per_prophecy()
    {
        var prophecy = CreateGatheringProphecy("{}");
        prophecy.TargetValue = 10;
        var repository = new ProgressRepository(prophecy);
        var service = CreateService(repository);
        var completionEventTime = Now.AddMinutes(10);

        var updates = await service.TrackProgressAsync(
        [
            new ProphecyProgressEvent(
                prophecy.CharacterId,
                completionEventTime,
                ProphecyProgressKind.ResourceGathered,
                Amount: 7,
                Profession: "Mining"),
            new ProphecyProgressEvent(
                prophecy.CharacterId,
                Now.AddMinutes(5),
                ProphecyProgressKind.ResourceGathered,
                Amount: 4,
                Profession: "Mining")
        ], CancellationToken.None);

        var update = Assert.Single(updates);
        Assert.Equal(1, repository.ProgressWindowQueryCount);
        Assert.Equal(10, prophecy.CurrentValue);
        Assert.Equal(10, update.AmountGained);
        Assert.True(update.Completed);
        Assert.Equal(ProphecyStatus.Completed.ToString(), update.Status);
        Assert.Equal(completionEventTime, prophecy.CompletedAt);
    }

    [Fact]
    public async Task ClaimAsync_credits_favor_to_week_containing_daily_prophecy()
    {
        var prophecy = CreateGatheringProphecy("{}");
        prophecy.Status = ProphecyStatus.Completed;
        prophecy.Scope = ProphecyScope.Daily;
        prophecy.PeriodStart = new DateTimeOffset(2026, 7, 12, 0, 0, 0, TimeSpan.Zero);
        prophecy.PeriodEnd = prophecy.PeriodStart.AddDays(1);
        prophecy.CompletedAt = prophecy.PeriodStart.AddHours(12);
        prophecy.RewardSnapshotJson = JsonSerializer.Serialize(
            new ProphecyRewardSnapshot(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var repository = new ProgressRepository(prophecy);
        var service = CreateService(repository);

        var result = await service.ClaimAsync(
            prophecy.PlayerId,
            prophecy.CharacterId,
            prophecy.Id,
            Now,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var claim = Assert.IsType<ProphecyClaimResult>(result.Value);
        var expectedWeekStart = new DateTimeOffset(2026, 7, 6, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(expectedWeekStart, repository.RequestedWeeklyPeriodStart);
        Assert.Equal(expectedWeekStart, claim.WeeklyRevelation.PeriodStart);
        Assert.Equal(1, claim.WeeklyRevelation.PropheticFavor);
        Assert.Equal(ProphecyStatus.Claimed, prophecy.Status);
    }

    [Fact]
    public async Task ClaimAsync_credits_two_favor_for_greater_prophecy()
    {
        var prophecy = CreateGatheringProphecy("{}");
        prophecy.Status = ProphecyStatus.Completed;
        prophecy.Scope = ProphecyScope.Weekly;
        prophecy.SlotType = ProphecySlotType.Greater;
        prophecy.PeriodStart = new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero);
        prophecy.PeriodEnd = prophecy.PeriodStart.AddDays(7);
        prophecy.CompletedAt = Now.AddHours(-1);
        prophecy.RewardSnapshotJson = JsonSerializer.Serialize(
            new ProphecyRewardSnapshot(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var repository = new ProgressRepository(prophecy);
        repository.WeeklyProgress = new WeeklyRevelationProgress
        {
            Id = Guid.NewGuid(),
            PlayerId = prophecy.PlayerId,
            CharacterId = prophecy.CharacterId,
            PeriodStart = prophecy.PeriodStart,
            PeriodEnd = prophecy.PeriodEnd,
            PropheticFavor = 5,
            CreatedAt = prophecy.PeriodStart
        };
        var service = CreateService(repository);

        var result = await service.ClaimAsync(
            prophecy.PlayerId,
            prophecy.CharacterId,
            prophecy.Id,
            Now,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var claim = Assert.IsType<ProphecyClaimResult>(result.Value);
        Assert.Equal(prophecy.PeriodStart, repository.RequestedWeeklyPeriodStart);
        Assert.Equal(2, claim.Reward.PropheticFavor);
        Assert.Equal(7, claim.WeeklyRevelation.PropheticFavor);
        Assert.Equal(ProphecyStatus.Claimed, prophecy.Status);
    }

    [Fact]
    public async Task ClaimAsync_rejects_replay_without_granting_favor_twice()
    {
        var prophecy = CreateCompletedProphecy();
        var repository = new ProgressRepository(prophecy);
        var service = CreateService(repository);

        var first = await service.ClaimAsync(
            prophecy.PlayerId,
            prophecy.CharacterId,
            prophecy.Id,
            Now,
            CancellationToken.None);
        var replay = await service.ClaimAsync(
            prophecy.PlayerId,
            prophecy.CharacterId,
            prophecy.Id,
            Now.AddMinutes(1),
            CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.False(replay.Succeeded);
        Assert.Equal("This prophecy has already been claimed.", replay.Error);
        Assert.Equal(1, repository.WeeklyProgress?.PropheticFavor);
    }

    [Fact]
    public async Task ClaimAsync_rejects_a_prophecy_owned_by_another_player_or_character()
    {
        var prophecy = CreateCompletedProphecy();
        var service = CreateService(prophecy);

        var wrongPlayer = await service.ClaimAsync(
            Guid.NewGuid(),
            prophecy.CharacterId,
            prophecy.Id,
            Now,
            CancellationToken.None);
        var wrongCharacter = await service.ClaimAsync(
            prophecy.PlayerId,
            Guid.NewGuid(),
            prophecy.Id,
            Now,
            CancellationToken.None);

        Assert.False(wrongPlayer.Succeeded);
        Assert.False(wrongCharacter.Succeeded);
        Assert.Equal(ProphecyStatus.Completed, prophecy.Status);
    }

    [Fact]
    public async Task ClaimWeeklyMilestoneAsync_enforces_unlock_known_tier_and_single_claim_rules()
    {
        var prophecy = CreateProphecy(ProphecyObjectiveType.KillCreatures);
        var weekStart = new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero);
        var repository = new ProgressRepository(prophecy)
        {
            WeeklyProgress = new WeeklyRevelationProgress
            {
                Id = Guid.NewGuid(),
                PlayerId = prophecy.PlayerId,
                CharacterId = prophecy.CharacterId,
                PeriodStart = weekStart,
                PeriodEnd = weekStart.AddDays(7),
                PropheticFavor = 5,
                CreatedAt = weekStart
            }
        };
        var service = CreateService(repository);

        var claimed = await service.ClaimWeeklyMilestoneAsync(
            prophecy.PlayerId,
            prophecy.CharacterId,
            3,
            Now,
            CancellationToken.None);
        var replay = await service.ClaimWeeklyMilestoneAsync(
            prophecy.PlayerId,
            prophecy.CharacterId,
            3,
            Now,
            CancellationToken.None);
        var locked = await service.ClaimWeeklyMilestoneAsync(
            prophecy.PlayerId,
            prophecy.CharacterId,
            7,
            Now,
            CancellationToken.None);
        var unknown = await service.ClaimWeeklyMilestoneAsync(
            prophecy.PlayerId,
            prophecy.CharacterId,
            4,
            Now,
            CancellationToken.None);

        Assert.True(claimed.Succeeded);
        Assert.True(repository.WeeklyProgress.Milestone3Claimed);
        Assert.False(replay.Succeeded);
        Assert.False(locked.Succeeded);
        Assert.False(unknown.Succeeded);
    }

    private static PlayerProphecyInstance CreateGatheringProphecy(string parameters)
        => CreateProphecy(ProphecyObjectiveType.GatherResources, parameters);

    private static PlayerProphecyInstance CreateProphecy(
        string objectiveType,
        string parameters = "{}")
    {
        var definition = new ProphecyDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "Test prophecy",
            ObjectiveType = objectiveType
        };

        return new PlayerProphecyInstance
        {
            Id = Guid.NewGuid(),
            CharacterId = Guid.NewGuid(),
            PlayerId = Guid.NewGuid(),
            ProphecyDefinitionId = definition.Id,
            ProphecyDefinition = definition,
            Status = ProphecyStatus.Accepted,
            AcceptedAt = Now.AddHours(-1),
            PeriodStart = Now.AddDays(-1),
            PeriodEnd = Now.AddDays(1),
            TargetValue = 100,
            ObjectiveParameterSnapshotJson = parameters
        };
    }

    private static PlayerProphecyInstance CreateCompletedProphecy()
    {
        var prophecy = CreateProphecy(ProphecyObjectiveType.GatherResources);
        prophecy.Status = ProphecyStatus.Completed;
        prophecy.Scope = ProphecyScope.Daily;
        prophecy.PeriodStart = new DateTimeOffset(2026, 7, 14, 0, 0, 0, TimeSpan.Zero);
        prophecy.PeriodEnd = prophecy.PeriodStart.AddDays(1);
        prophecy.CompletedAt = Now;
        prophecy.RewardSnapshotJson = "{}";
        return prophecy;
    }

    private static ProphecyService CreateService(PlayerProphecyInstance prophecy) =>
        CreateService(new ProgressRepository(prophecy));

    private static ProphecyService CreateService(IProphecyRepository repository) =>
        new(
            new EmptyDefinitionProvider(),
            new TestBalanceProvider(),
            null!,
            new ExperienceProgressionProvider(),
            repository,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

    private sealed class EmptyDefinitionProvider : IProphecyDefinitionProvider
    {
        public IReadOnlyList<ProphecyDefinition> GetAll() => [];
    }

    private sealed class ExperienceProgressionProvider : ICharacterExperienceProgressionProvider
    {
        public long GetRequiredExperience(int level) => 125;
    }

    private sealed class TestBalanceProvider : IProphecyBalanceProvider
    {
        public ProphecyBalanceCatalog GetCatalog() => new()
        {
            FavorRewards =
            [
                new ProphecyFavorReward { Scope = ProphecyScope.Daily, Amount = 1 },
                new ProphecyFavorReward { Scope = ProphecyScope.Weekly, Amount = 2 }
            ],
            WeeklyMilestones =
            [
                new ProphecyWeeklyMilestoneDefinition { FavorRequired = 3, Title = "Small Revelation Cache" },
                new ProphecyWeeklyMilestoneDefinition { FavorRequired = 5, Title = "Greater Revelation Cache" },
                new ProphecyWeeklyMilestoneDefinition { FavorRequired = 7, Title = "Perfect Week Bonus" }
            ]
        };
    }

    private sealed class ProgressRepository(PlayerProphecyInstance prophecy) : IProphecyRepository
    {
        public int ProgressWindowQueryCount { get; private set; }
        public DateTimeOffset? RequestedWeeklyPeriodStart { get; private set; }
        public WeeklyRevelationProgress? WeeklyProgress { get; set; }

        public Task<IReadOnlyList<PlayerProphecyInstance>> GetAcceptedInstancesForProgressWindowAsync(
            Guid characterId,
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken cancellationToken)
        {
            ProgressWindowQueryCount++;
            return Task.FromResult<IReadOnlyList<PlayerProphecyInstance>>(
                prophecy.CharacterId == characterId ? [prophecy] : []);
        }

        public Task<IReadOnlyList<ProphecyDefinition>> GetEnabledDefinitionsAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ProphecyDefinition>> SyncDefinitionsAsync(
            IReadOnlyCollection<ProphecyDefinition> definitions,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlayerProphecyInstance>> GetInstancesForPeriodAsync(
            Guid playerId,
            Guid characterId,
            ProphecyScope scope,
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PlayerProphecyInstance?> GetInstanceAsync(
            Guid instanceId,
            Guid playerId,
            Guid characterId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PlayerProphecyInstance?>(
                prophecy.Id == instanceId &&
                prophecy.PlayerId == playerId &&
                prophecy.CharacterId == characterId
                    ? prophecy
                    : null);

        public Task AddInstancesAsync(
            IReadOnlyCollection<PlayerProphecyInstance> instances,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlayerProphecyInstance>> GetAcceptedInstancesForProgressAsync(
            Guid characterId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> TryConsumeDailyRerollAsync(
            Guid playerId,
            Guid characterId,
            DateTimeOffset periodStart,
            DateTimeOffset usedAt,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> TrySpendFateEchoAsync(
            Guid characterId,
            long amount,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<WeeklyRevelationProgress?> GetWeeklyProgressAsync(
            Guid playerId,
            Guid characterId,
            DateTimeOffset periodStart,
            CancellationToken cancellationToken)
        {
            RequestedWeeklyPeriodStart = periodStart;
            return Task.FromResult(
                WeeklyProgress is not null &&
                WeeklyProgress.PlayerId == playerId &&
                WeeklyProgress.CharacterId == characterId &&
                WeeklyProgress.PeriodStart == periodStart
                    ? WeeklyProgress
                    : null);
        }

        public Task AddWeeklyProgressAsync(
            WeeklyRevelationProgress progress,
            CancellationToken cancellationToken)
        {
            WeeklyProgress = progress;
            return Task.CompletedTask;
        }
    }
}
