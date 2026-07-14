using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Prophecies;
using Services.LL.Prophecies;

namespace EssenceSystem.Tests;

public sealed class ProphecyServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

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

    private static PlayerProphecyInstance CreateGatheringProphecy(string parameters)
    {
        var definition = new ProphecyDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "Gather resources",
            ObjectiveType = ProphecyObjectiveType.GatherResources
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

    private static ProphecyService CreateService(PlayerProphecyInstance prophecy) =>
        new(
            new EmptyDefinitionProvider(),
            new ProgressRepository(prophecy),
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

    private sealed class ProgressRepository(PlayerProphecyInstance prophecy) : IProphecyRepository
    {
        public Task<IReadOnlyList<PlayerProphecyInstance>> GetAcceptedInstancesForProgressWindowAsync(
            Guid characterId,
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlayerProphecyInstance>>(
                prophecy.CharacterId == characterId ? [prophecy] : []);

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
            throw new NotSupportedException();

        public Task AddInstancesAsync(
            IReadOnlyCollection<PlayerProphecyInstance> instances,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlayerProphecyInstance>> GetAcceptedInstancesForProgressAsync(
            Guid characterId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlayerProphecyInstance>> GetRecentInstancesAsync(
            Guid playerId,
            Guid characterId,
            DateTimeOffset since,
            int limit,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<WeeklyRevelationProgress?> GetWeeklyProgressAsync(
            Guid playerId,
            Guid characterId,
            DateTimeOffset periodStart,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddWeeklyProgressAsync(
            WeeklyRevelationProgress progress,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
