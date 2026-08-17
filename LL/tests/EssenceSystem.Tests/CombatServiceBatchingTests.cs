using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Options;
using Services.LL.CharacterActions;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Reward;

namespace EssenceSystem.Tests;

public sealed class CombatServiceBatchingTests
{
    private static readonly DateTimeOffset FirstBoundary =
        DateTimeOffset.Parse("2026-08-17T12:00:00Z");

    [Fact]
    public async Task Offline_progress_is_resolved_in_internal_batches_and_returned_once()
    {
        var coordinator = new BatchingCoordinator(batchSize: 100, cadenceSeconds: 10);
        var outcome = new OutcomeCoordinatorStub();
        var service = CreateService(coordinator, outcome, maximumBatches: 10);
        var action = CreateAction();

        var session = await service.PerformIdleCombatAsync(
            action,
            FirstBoundary.AddSeconds(2_490),
            CancellationToken.None);

        Assert.Equal(3, coordinator.CallCount);
        Assert.Equal(3, outcome.CallCount);
        Assert.Equal(250, action.ProcessedCount);
        Assert.False(action.HasMoreDueWork);
        Assert.Equal(FirstBoundary.AddSeconds(2_500), action.NextResolutionAtUtc);
        Assert.Equal(250, session.CombatSummary.TotalBattles);
        Assert.Equal(250, session.CombatSummary.TotalExperience);
        Assert.Equal(250, session.CombatResult.ExperienceGained);
        Assert.Equal(250, Assert.Single(session.CombatResult.Loot).Quantity);
        Assert.Equal(
            250,
            Assert.Single(session.CombatSummary.RewardBreakdown.CraftingItems).Quantity);
    }

    [Fact]
    public async Task Internal_batch_limit_remains_an_explicit_safety_boundary()
    {
        var coordinator = new BatchingCoordinator(batchSize: 100, cadenceSeconds: 10);
        var service = CreateService(
            coordinator,
            new OutcomeCoordinatorStub(),
            maximumBatches: 2);
        var action = CreateAction();

        var session = await service.PerformIdleCombatAsync(
            action,
            FirstBoundary.AddHours(1),
            CancellationToken.None);

        Assert.Equal(2, coordinator.CallCount);
        Assert.Equal(200, action.ProcessedCount);
        Assert.True(action.HasMoreDueWork);
        Assert.Equal(200, session.CombatSummary.TotalBattles);
    }

    [Fact]
    public async Task Default_batch_capacity_covers_the_complete_24_hour_window()
    {
        var coordinator = new BatchingCoordinator(batchSize: 100, cadenceSeconds: 10);
        var service = CreateService(
            coordinator,
            new OutcomeCoordinatorStub(),
            maximumBatches: 100);
        var action = CreateAction();

        var session = await service.PerformIdleCombatAsync(
            action,
            FirstBoundary.AddHours(24),
            CancellationToken.None);

        Assert.Equal(87, coordinator.CallCount);
        Assert.Equal(8_641, action.ProcessedCount);
        Assert.False(action.HasMoreDueWork);
        Assert.Equal(8_641, session.CombatSummary.TotalBattles);
    }

    private static CombatService CreateService(
        ICombatOrchestrationCoordinator coordinator,
        ICombatOutcomeCoordinator outcome,
        int maximumBatches) =>
        new(
            coordinator,
            outcome,
            options: Options.Create(new IdleCombatProgressionOptions
            {
                EncounterCadenceSeconds = 10,
                MaximumOfflineHours = 24,
                MaximumEncountersPerResolution = 100,
                MaximumBatchesPerResolution = maximumBatches
            }));

    private static CharacterAction CreateAction() =>
        new(Guid.NewGuid(), new CombatActionDetails(
            [Guid.NewGuid()],
            new Area { Id = "test-area" }), FirstBoundary);

    private sealed class BatchingCoordinator(
        int batchSize,
        int cadenceSeconds) : ICombatOrchestrationCoordinator
    {
        private readonly TimeSpan _cadence = TimeSpan.FromSeconds(cadenceSeconds);

        public int CallCount { get; private set; }

        public Task<CombatOrchestrationResult> OrchestrateAsync(
            CombatOrchestrationRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            var idle = Assert.IsType<IdleCombatOrchestrationRequest>(request);
            var boundary = idle.NextEncounterAt;
            var due = idle.Now < boundary
                ? 0
                : checked(1 + (int)((idle.Now - boundary).Ticks / _cadence.Ticks));
            var count = Math.Min(batchSize, due);
            var processedUntil = boundary.AddTicks(count * _cadence.Ticks);
            var records = Enumerable.Range(0, count)
                .Select(_ => new CombatEncounterRecord(null!, null!))
                .ToArray();

            return Task.FromResult(new CombatOrchestrationResult(
                Guid.NewGuid(),
                CombatMode.Idle,
                records,
                new IdleCombatOrchestrationDetails(
                    boundary,
                    idle.Now,
                    processedUntil,
                    count,
                    _cadence)));
        }
    }

    private sealed class OutcomeCoordinatorStub : ICombatOutcomeCoordinator
    {
        public int CallCount { get; private set; }

        public Task<CombatSession> ApplyAsync(
            CombatOutcomeRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            var details = Assert.IsType<IdleCombatOrchestrationDetails>(
                request.OrchestrationResult.Details);
            var count = request.OrchestrationResult.EncounterCount;
            var item = CreateItem(count);

            return Task.FromResult(new CombatSession
            {
                From = details.From,
                To = details.ProcessedUntil,
                CombatResult = new CombatResult
                {
                    StartedAt = details.From,
                    ExperienceGained = count,
                    Loot = [item]
                },
                CombatSummary = new CombatSummary
                {
                    TotalBattles = count,
                    Wins = count,
                    TotalExperience = count,
                    RewardBreakdown = new CombatRewardBreakdown
                    {
                        CraftingItems = [item]
                    }
                }
            });
        }

        private static InventoryItem CreateItem(int quantity) =>
            new()
            {
                Quantity = quantity,
                ItemInstance = new ItemInstance
                {
                    Id = Guid.NewGuid(),
                    ItemBaseId = "ore",
                    ItemBase = new ItemBase { Id = "ore", Name = "Ore" }
                }
            };
    }
}
