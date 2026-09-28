using Application.UseCases.Colosseum.Commands.UpdateArenaDefenseSnapshot;
using Application.UseCases.Colosseum.Dtos;
using Application.UseCases.Equipments.Commands.ConvertEquipmentOnStartup;
using Common.Primitives;
using Domain.Models.Attributes;
using Domain.Models.Items.Equipments.Progression;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class EquipmentStartupConversionTests
{
    [Fact]
    public async Task Restart_resumes_remaining_pages_then_refreshes_and_verifies_without_repeating_completed_work()
    {
        var store = new Store();
        store.Targets.AddRange(Enumerable.Range(0, 205).Select(_ => new EquipmentMigrationTarget(Guid.NewGuid())));
        store.Defenses.Add(Guid.NewGuid());
        using var services = Provider(store);
        store.FailAtConversion = 103;
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Runner(services).RunAsync(default));
        Assert.Contains("restarting retries", error.Message);
        Assert.Equal(102, store.Converted);
        Assert.Equal(103, store.Targets.Count);
        Assert.Equal(0, store.Refreshed);
        Assert.False(store.LockHeld);

        store.FailAtConversion = null;
        await Runner(services).RunAsync(default);
        Assert.Equal(205, store.Converted);
        Assert.Equal(1, store.Refreshed);
        Assert.Empty(store.Targets);
        Assert.Empty(store.Defenses);
        Assert.True(store.Audited);
        await Runner(services).RunAsync(default);
        Assert.Equal(205, store.Converted);
        Assert.Equal(1, store.Refreshed);
        Assert.Equal(3, store.LockAcquisitions);
    }

    [Theory]
    [InlineData(1, 0, 0, 0)]
    [InlineData(0, 1, 0, 0)]
    [InlineData(0, 0, 1, 0)]
    [InlineData(0, 0, 0, 1)]
    public async Task Verification_fails_for_each_unresolved_live_population(int items, int rewards, int defenses, int tournaments)
    {
        var store = new Store { FinalAudit = new(items, rewards, defenses, tournaments, 408) };
        using var services = Provider(store);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Runner(services).RunAsync(default));
        Assert.Contains("verification failed", error.Message);
        Assert.False(store.LockHeld);
    }

    [Fact]
    public async Task Failed_Arena_refresh_is_not_reported_as_success()
    {
        var store = new Store { FailRefresh = true };
        store.Defenses.Add(Guid.NewGuid());
        using var services = Provider(store);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Runner(services).RunAsync(default));
        Assert.False(store.Audited);
        Assert.Single(store.Defenses);
        Assert.False(store.LockHeld);
    }

    [Fact]
    public async Task Disabled_startup_and_mismatched_selectors_do_not_acquire_locks_or_write()
    {
        var store = new Store();
        using var services = Provider(store);
        await Runner(services, enabled: false).RunAsync(default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Runner(services, rules: new(17, 1)).RunAsync(default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Runner(services, healing: "").RunAsync(default));
        Assert.Equal(0, store.LockAcquisitions);
        Assert.False(store.Audited);
    }

    private static ServiceProvider Provider(Store store) => new ServiceCollection()
        .AddSingleton<IEquipmentStartupConversionRepository>(store)
        .AddScoped<ISender>(_ => new Sender(store)).BuildServiceProvider();

    private static EquipmentStartupConversion Runner(ServiceProvider services, bool enabled = true, AttributeRulesSelection? rules = null,
        string healing = "healing-v1") =>
        new(services.GetRequiredService<IServiceScopeFactory>(), new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EquipmentConversion:RunOnStartup"] = enabled.ToString(),
                ["EquipmentConversion:TargetBalanceVersion"] = "4",
                ["Combat:AbilityBalanceProfile"] = healing
            }).Build(), rules ?? new(18, 4), NullLogger<EquipmentStartupConversion>.Instance);

    private sealed class Store : IEquipmentStartupConversionRepository
    {
        public List<EquipmentMigrationTarget> Targets { get; } = [];
        public List<Guid> Defenses { get; } = [];
        public int Converted, Refreshed, LockAcquisitions;
        public bool LockHeld, Audited, FailRefresh;
        public int? FailAtConversion;
        public EquipmentStartupConversionAudit? FinalAudit;
        public Task<IAsyncDisposable> AcquireRunnerLockAsync(CancellationToken ct)
        {
            LockAcquisitions++;
            LockHeld = true;
            return Task.FromResult<IAsyncDisposable>(new Lease(this));
        }
        public Task<IReadOnlyList<EquipmentMigrationTarget>> GetTargetsAsync(int version, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<EquipmentMigrationTarget>>(Targets.Take(limit).ToArray());
        public Task<IReadOnlyList<Guid>> GetArenaDefensesAsync(int version, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Guid>>(Defenses.Take(limit).ToArray());
        public Task<EquipmentStartupConversionAudit> AuditAsync(int version, CancellationToken ct)
        {
            Audited = true;
            return Task.FromResult(FinalAudit ?? new(Targets.Count, 0, Defenses.Count, 0, 408));
        }
        public Task LockCharactersAsync(EquipmentMigrationTarget target, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> IsCandidateAsync(EquipmentMigrationTarget target, int version, CancellationToken ct) => throw new NotSupportedException();
        private sealed class Lease(Store store) : IAsyncDisposable
        {
            public ValueTask DisposeAsync() { store.LockHeld = false; return ValueTask.CompletedTask; }
        }
    }

    private sealed class Sender(Store store) : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Assert.True(store.LockHeld);
            if (request is ConvertEquipmentOnStartupCommand convert)
            {
                if (store.FailAtConversion == store.Converted + 1) throw new InvalidOperationException("Interruption");
                Assert.Equal(4, convert.TargetBalanceVersion);
                Assert.True(store.Targets.Remove(convert.Target));
                store.Converted++;
                return Task.FromResult((TResponse)(object)true);
            }
            if (request is UpdateArenaDefenseSnapshotCommand refresh)
            {
                Assert.Empty(store.Targets);
                if (store.FailRefresh) return Task.FromResult((TResponse)(object)Response<ArenaDefenseStatusDto>.Fail("Refresh failed"));
                Assert.True(store.Defenses.Remove(refresh.CharacterId));
                store.Refreshed++;
                return Task.FromResult((TResponse)(object)Response<ArenaDefenseStatusDto>.Success(new()));
            }
            throw new NotSupportedException();
        }
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
