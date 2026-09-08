using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.Guilds;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Guilds.Missions;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Dungeons;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Dungeons;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace EssenceSystem.Tests;

public sealed class DungeonRunHardeningTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Treasury_costs_exact_vigor_awards_once_and_can_be_secured_by_retreat(bool chooseRoute)
    {
        var run = CreateTreasuryRun(chooseRoute, 19);
        var acquisition = new TreasuryAcquisition();
        var service = TreasuryService(run, acquisition);
        var action = chooseRoute ? "choose_route" : "open_treasury";
        var payload = chooseRoute ? new { routeOptionId = "treasury-route" } : null;

        var result = await service.ExecuteActionAsync(run.CharacterId, run.Id, action, payload, default);
        Assert.Equal(DungeonActionOutcome.TreasuryOpened, result!.Outcome);
        Assert.Equal(1, run.State.Vigor);
        Assert.Equal(-18, Assert.Single(run.State.VigorHistory).Amount);
        Assert.Equal(RoomInstanceStatus.Completed, run.Rooms[1].Status);
        Assert.Single(run.PendingRewards);
        Assert.Equal(1, acquisition.Rolls);

        await service.ExecuteActionAsync(run.CharacterId, run.Id, "open_treasury", null, default);
        Assert.Single(run.PendingRewards);
        Assert.Equal(1, acquisition.Rolls);
        Assert.Equal(1, run.State.Vigor);

        await service.ExecuteActionAsync(run.CharacterId, run.Id, "retreat", null, default);
        Assert.Equal(DungeonRunStatus.Retreated, run.Status);
        Assert.Equal(1, run.State.SecuredLoot.Items["item.blueprint_fury"]);
    }

    [Theory]
    [InlineData(false, 18)]
    [InlineData(true, 18)]
    [InlineData(true, 10)]
    public async Task Unaffordable_treasury_does_not_move_the_player_or_award_loot(bool chooseRoute, int vigor)
    {
        var run = CreateTreasuryRun(chooseRoute, vigor);
        var acquisition = new TreasuryAcquisition();
        var service = TreasuryService(run, acquisition);
        var originalIndex = run.CurrentRoomIndex;

        var result = await service.ExecuteActionAsync(run.CharacterId, run.Id,
            chooseRoute ? "choose_route" : "open_treasury",
            chooseRoute ? new { routeOptionId = "TREASURY-ROUTE" } : null, default);

        Assert.Equal(DungeonActionOutcome.None, result!.Outcome);
        Assert.Equal(originalIndex, run.CurrentRoomIndex);
        Assert.Equal(vigor, run.State.Vigor);
        Assert.Empty(run.PendingRewards);
        Assert.Empty(run.State.VigorHistory);
        Assert.Equal(0, acquisition.Rolls);
        Assert.Equal(RoomInstanceStatus.Pending, run.Rooms[1].Status);
        if (chooseRoute) Assert.NotEmpty(run.State.CurrentRouteOptions);
    }

    [Fact]
    public void Treasury_forecasts_ignore_combat_scaling_and_mastery_discounts()
    {
        var run = CreateTreasuryRun(true, 20);
        run.State.MasteryLevelAtStart = 10;
        run.State.VigorState = "Exhausted";
        var route = Assert.Single(new DungeonRouteService().GenerateRouteOptions(run));
        Assert.Equal(18, route.VigorCostMin);
        Assert.Equal(18, route.VigorCostMax);
    }

    private static DungeonRun CreateTreasuryRun(bool chooseRoute, int vigor)
    {
        var run = CreateCompletedRun();
        run.Status = DungeonRunStatus.Active;
        run.CompletedAt = null;
        run.CurrentRoomIndex = chooseRoute ? 0 : 1;
        run.Rooms =
        [
            new() { RoomIndex = 0, Type = RoomType.Entrance, Status = RoomInstanceStatus.Completed },
            new() { RoomIndex = 1, Type = RoomType.Treasury },
            new() { RoomIndex = 2, Type = RoomType.Boss }
        ];
        run.State = new DungeonRunState
        {
            Vigor = vigor, ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            MapNodes =
            [
                new() { Id = "entrance", RoomIndex = 0, Depth = 0, Section = 1, NextRoomIndexes = [1] },
                new() { Id = "treasury", RoomIndex = 1, Depth = 1, Section = 1, VigorCostMin = 18, VigorCostMax = 18, NextRoomIndexes = [2] },
                new() { Id = "boss", RoomIndex = 2, Depth = 2, Section = 1 }
            ],
            CurrentRouteOptions = chooseRoute
                ? [new() { Id = "treasury-route", RoomIndex = 1, RoomType = RoomType.Treasury }]
                : []
        };
        return run;
    }

    private static DungeonRunService TreasuryService(DungeonRun run, IEquipmentAcquisitionService acquisition) =>
        new(new FixedDungeonRunRepository(run), null!, null!, null!, null!, null!, null!, null!, null!,
            new DungeonVigorService(), new DungeonRouteService(), new RecordingGuildMissionService(), null!, acquisition);

    private sealed class TreasuryAcquisition : IEquipmentAcquisitionService
    {
        public int Rolls { get; private set; }
        public Task CompleteAsync(DungeonRun run, bool firstCompletion, CancellationToken ct) => throw new NotSupportedException();
        public RunReward RollTreasuryReward(DungeonRun run, int roomIndex)
        {
            Rolls++;
            return new RunReward { ItemId = "item.blueprint_fury", Name = "Blueprint: Fury", Quantity = 1 };
        }
    }

    [Fact]
    public void Model_allows_only_one_run_per_character_and_tracks_concurrency()
    {
        using var context = CreateDbContext(Guid.NewGuid().ToString());
        var entity = context.Model.FindEntityType(typeof(DungeonRun))!;

        Assert.True(entity.FindProperty(nameof(DungeonRun.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(DungeonRun.CharacterId)]));
    }

    [Fact]
    public async Task Concurrent_reward_claim_deletes_cannot_both_commit()
    {
        var databaseName = Guid.NewGuid().ToString();
        var run = CreateCompletedRun();
        await using (var seedContext = CreateDbContext(databaseName))
        {
            seedContext.DungeonRuns.Add(run);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = CreateDbContext(databaseName);
        await using var secondContext = CreateDbContext(databaseName);
        var firstRepository = new DungeonRunRepository(firstContext);
        var secondRepository = new DungeonRunRepository(secondContext);
        var firstRun = await firstRepository.GetDungeonRunByCharacterIdAsync(
            run.CharacterId,
            CancellationToken.None);
        var secondRun = await secondRepository.GetDungeonRunByCharacterIdAsync(
            run.CharacterId,
            CancellationToken.None);

        await firstRepository.DeleteDungeonRunAsync(firstRun!, CancellationToken.None);
        await firstContext.SaveChangesAsync();
        await secondRepository.DeleteDungeonRunAsync(secondRun!, CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Completion_currency_update_is_persisted()
    {
        var databaseName = Guid.NewGuid().ToString();
        var run = CreateCompletedRun();
        await using (var seedContext = CreateDbContext(databaseName))
        {
            seedContext.DungeonRuns.Add(run);
            await seedContext.SaveChangesAsync();
        }

        await using (var updateContext = CreateDbContext(databaseName))
        {
            var repository = new DungeonRunRepository(updateContext);
            var loaded = await repository.GetDungeonRunByDungeonIdAsync(
                run.Id,
                CancellationToken.None);
            loaded!.PendingCinders = 75;

            Assert.True(await repository.UpdateDungeonRunAsync(
                loaded,
                CancellationToken.None));
            await updateContext.SaveChangesAsync();
        }

        await using var verificationContext = CreateDbContext(databaseName);
        Assert.Equal(75, (await verificationContext.DungeonRuns.SingleAsync()).PendingCinders);
    }

    [Fact]
    public async Task Another_character_cannot_execute_or_claim_a_run()
    {
        var run = CreateCompletedRun();
        run.Status = DungeonRunStatus.Active;
        var repository = new FixedDungeonRunRepository(run);
        var service = new DungeonRunService(
            repository,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
        var otherCharacterId = Guid.NewGuid();

        var action = await service.ExecuteActionAsync(
            otherCharacterId,
            run.Id,
            "fight",
            null,
            CancellationToken.None);
        run.Status = DungeonRunStatus.Completed;
        var claim = await service.ClaimRewardsAsync(
            otherCharacterId,
            CancellationToken.None);

        Assert.Null(action);
        Assert.Null(claim);
        Assert.Equal(0U, run.RowVersion);
    }

    [Fact]
    public async Task Loading_a_run_repairs_a_missing_single_route_without_advancing_the_player()
    {
        var run = CreateCompletedRun();
        run.Status = DungeonRunStatus.Active;
        run.CompletedAt = null;
        run.CurrentRoomIndex = 0;
        run.Rooms =
        [
            new RoomInstance
            {
                RoomIndex = 0,
                Type = Domain.Models.Dungeons.Definitions.Rooms.RoomType.Entrance,
                Status = RoomInstanceStatus.Completed
            },
            new RoomInstance
            {
                RoomIndex = 1,
                Type = Domain.Models.Dungeons.Definitions.Rooms.RoomType.Combat,
                Status = RoomInstanceStatus.Pending
            }
        ];
        run.State.MapNodes =
        [
            new DungeonMapNode
            {
                Id = "entrance",
                RoomIndex = 0,
                NextRoomIndexes = [1]
            },
            new DungeonMapNode
            {
                Id = "first-combat",
                DisplayName = "First Combat",
                RoomIndex = 1,
                Depth = 1,
                VigorCostMin = 10,
                VigorCostMax = 18
            }
        ];
        var service = new DungeonRunService(
            new FixedDungeonRunRepository(run),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new DungeonVigorService(),
            new DungeonRouteService(),
            null!,
            null!);

        var loaded = await service.GetDungeonRunAsync(
            run.CharacterId,
            CancellationToken.None);

        Assert.NotNull(loaded);
        var route = Assert.Single(loaded.State.CurrentRouteOptions);
        Assert.Equal(1, route.RoomIndex);
        Assert.Equal(0, loaded.CurrentRoomIndex);
    }

    [Fact]
    public async Task Completing_final_combat_records_dungeon_completion_for_guild_mission()
    {
        var run = new DungeonRun
        {
            Id = Guid.NewGuid(),
            CharacterId = Guid.NewGuid(),
            DungeonDefinitionId = "goblin_mines",
            DungeonDefinitionName = "Goblin Mines I",
            Status = DungeonRunStatus.Active,
            CurrentRoomIndex = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            Rooms =
            [
                new RoomInstance
                {
                    RoomIndex = 0,
                    Type = RoomType.Boss,
                    Status = RoomInstanceStatus.Pending,
                    EncounterIds = ["goblin_king"]
                }
            ],
            State = new DungeonRunState
            {
                Vigor = 100,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                MapNodes =
                [
                    new DungeonMapNode
                    {
                        Id = "final-boss",
                        RoomIndex = 0,
                        Section = 1
                    }
                ],
                TraversedRoomIndexes = [0]
            }
        };
        var guildMissions = new RecordingGuildMissionService();
        var service = new DungeonRunService(
            new FixedDungeonRunRepository(run),
            new FixedCharacterSnapshotRepository(run.CharacterId),
            new StubCombatOrchestrationCoordinator(),
            new VictoryCombatOutcomeCoordinator(),
            null!,
            null!,
            new StubDungeonCompletionRewardApplier(),
            new FixedDungeonDefinitions(run.DungeonDefinitionId),
            null!,
            new DungeonVigorService(),
            new DungeonRouteService(),
            guildMissions,
            null!);

        var result = await service.ExecuteActionAsync(
            run.CharacterId,
            run.Id,
            "fight",
            null,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(DungeonActionOutcome.RunCompleted, result.Outcome);
        Assert.Equal(DungeonRunStatus.Completed, run.Status);
        Assert.Collection(
            guildMissions.Events,
            room => Assert.Equal(GuildContributionMetric.DungeonRoomsCleared, room.Metric),
            completion => Assert.Equal(GuildContributionMetric.DungeonsCompleted, completion.Metric));
    }

    private static LLDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new LLDbContext(options);
    }

    private static DungeonRun CreateCompletedRun() => new()
    {
        Id = Guid.NewGuid(),
        CharacterId = Guid.NewGuid(),
        DungeonDefinitionId = "goblin_mines",
        DungeonDefinitionName = "Goblin Mines I",
        Status = DungeonRunStatus.Completed,
        CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        CompletedAt = DateTimeOffset.UtcNow,
        State = new DungeonRunState()
    };

    private sealed class FixedDungeonRunRepository(DungeonRun run) : IDungeonRunRepository
    {
        public Task<DungeonRun?> GetDungeonRunByCharacterIdAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            Task.FromResult<DungeonRun?>(run.CharacterId == characterId ? run : null);

        public Task<DungeonRun?> GetDungeonRunByDungeonIdAsync(
            Guid dungeonId,
            CancellationToken cancellationToken) =>
            Task.FromResult<DungeonRun?>(run.Id == dungeonId ? run : null);

        public Task<bool> CreateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> DeleteDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> AddPendingRewardAsync(
            DungeonRun dungeonRun,
            RunReward reward,
            CancellationToken cancellationToken)
        {
            dungeonRun.PendingRewards.Add(reward);
            return Task.FromResult(true);
        }

        public Task<bool> HasActiveDungeonRunAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DungeonCompletionRecord>> GetCompletionRecordsAsync(
            Guid characterId,
            IReadOnlyCollection<string> dungeonDefinitionIds,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DungeonCompletionLeaderboardEntry>> GetCompletionLeaderboardAsync(
            IReadOnlyCollection<string> dungeonDefinitionIds,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> HasCompletedDungeonAsync(
            Guid characterId,
            string dungeonDefinitionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task MarkDungeonCompletedAsync(
            Guid characterId,
            string dungeonDefinitionId,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> UpdateDungeonRunAsync(
            DungeonRun dungeonRun,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FixedCharacterSnapshotRepository(Guid characterId) : ICharacterSnapshotRepository
    {
        private readonly CharacterSnapshot _snapshot = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = "Dungeon Tester",
            Level = 1
        };

        public Task<CharacterSnapshot> CreateAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(_snapshot);

        public Task<CharacterSnapshot?> GetSnapshotByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<CharacterSnapshot?>(_snapshot);

        public Task<CharacterSnapshot?> GetSnapshotByIdAsync(Guid snapshotId, CancellationToken cancellationToken) =>
            Task.FromResult<CharacterSnapshot?>(_snapshot);
    }

    private sealed class StubCombatOrchestrationCoordinator : ICombatOrchestrationCoordinator
    {
        public Task<CombatOrchestrationResult> OrchestrateAsync(
            CombatOrchestrationRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult<CombatOrchestrationResult>(null!);
    }

    private sealed class VictoryCombatOutcomeCoordinator : ICombatOutcomeCoordinator
    {
        public Task<CombatSession> ApplyAsync(
            CombatOutcomeRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new CombatSession
            {
                CombatResult = new CombatResult { Outcome = BattleOutcome.Victory }
            });
    }

    private sealed class StubDungeonCompletionRewardApplier : IDungeonCompletionRewardApplier
    {
        public Task ApplyAsync(DungeonRun run, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FixedDungeonDefinitions(string dungeonId) : IDungeonDefinitions
    {
        private readonly DungeonDefinition _definition = new()
        {
            Id = dungeonId,
            Name = "Goblin Mines I",
            Tier = 1
        };

        public DungeonDefinition GetByKey(string key) => _definition;

        public IReadOnlyList<DungeonDefinition> GetAll() => [_definition];
    }

    private sealed class RecordingGuildMissionService : IGuildMissionService
    {
        public List<GuildContributionEvent> Events { get; } = [];

        public Task<GuildContributionResult> RecordContributionAsync(
            GuildContributionEvent contributionEvent,
            CancellationToken cancellationToken)
        {
            Events.Add(contributionEvent);
            return Task.FromResult(new GuildContributionResult(true, false, contributionEvent.Amount, 0));
        }

        public Task<GuildMissionOverviewDto?> GetOverviewAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GuildOperationResult<GuildMissionOverviewDto>> SelectMissionAsync(Guid characterId, Guid missionOptionId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GuildOperationResult<GuildMissionOverviewDto>> ClaimPersonalOrderRewardAsync(Guid characterId, Guid orderId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GuildOperationResult<GuildMissionOverviewDto>> ClaimWeeklyRewardAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
