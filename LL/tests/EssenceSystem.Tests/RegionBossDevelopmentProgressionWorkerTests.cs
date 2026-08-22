using API.LL.HostedServices;
using Application.Interfaces.Services.LL.RegionBosses;
using Application.UseCases.RegionBosses.Dtos;
using Domain.Models.RegionBosses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Services.LL.RegionBosses;

namespace EssenceSystem.Tests;

public sealed class RegionBossDevelopmentProgressionWorkerTests
{
    [Fact]
    public async Task Enabled_worker_progresses_region_bosses_inside_the_api_process()
    {
        var recorder = new RecordingRegionBossService();
        var services = new ServiceCollection();
        services.AddScoped<IRegionBossService>(_ => recorder);
        await using var provider = services.BuildServiceProvider();
        using var worker = new RegionBossDevelopmentProgressionWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Options.Create(new RegionBossOptions
            {
                DevelopmentToolsEnabled = true,
                DevelopmentProgressionIntervalSeconds = 1
            }),
            NullLogger<RegionBossDevelopmentProgressionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        var workerId = await recorder.Progressed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal($"{Environment.MachineName}:api-development-region-boss", workerId);
        Assert.Equal(1, recorder.ProgressCalls);
    }

    private sealed class RecordingRegionBossService : IRegionBossService
    {
        public TaskCompletionSource<string> Progressed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ProgressCalls { get; private set; }

        public Task ProgressEventsAsync(string workerId, CancellationToken cancellationToken)
        {
            ProgressCalls++;
            Progressed.TrySetResult(workerId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RegionBossStatusDto>> GetStatusAsync(
            Guid characterId,
            int? regionId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RegionBossStatusDto?> GetEventAsync(
            Guid characterId,
            Guid eventId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RegionBossOperationResult<RegionBossStatusDto>> SignupAsync(
            Guid characterId,
            Guid eventId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RegionBossOperationResult<RegionBossStatusDto>> WithdrawAsync(
            Guid characterId,
            Guid eventId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RegionBossOperationResult<RegionBossClaimResultDto>> ClaimAsync(
            Guid characterId,
            Guid grantId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RegionBossOperationResult<RegionBossStatusDto>> SpawnDevelopmentEventAsync(
            Guid characterId,
            int regionId,
            int additionalSignupCount,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RegionBossPlaybackDto?> GetPlaybackAsync(
            Guid characterId,
            Guid runId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RegionBossPlaybackBundleContentDto?> GetPlaybackBundleAsync(
            Guid characterId,
            Guid runId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task EnsureScheduledEventsAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
