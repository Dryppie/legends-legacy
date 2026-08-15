using Application.BackgroundJobs;
using Application.Interfaces.Services.LL.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Combat;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quartz;
using Services.LL.Colosseum.Tournaments;
using Worker.LL.BackgroundJobs;

namespace EssenceSystem.Tests;

public sealed class TournamentGroundsProgressionJobTests
{
    [Fact]
    public async Task Execute_WhenEnabled_RunsTournamentProgressionThroughExecutionService()
    {
        var tournamentGrounds = new CapturingTournamentGroundsService();
        var executionService = new CapturingBackgroundJobExecutionService();
        var job = new TournamentGroundsProgressionJob(
            tournamentGrounds,
            executionService,
            Options.Create(new TournamentGroundsOptions { Enabled = true }),
            NullLogger<TournamentGroundsProgressionJob>.Instance);

        await job.Execute(new FakeJobExecutionContext());

        Assert.Equal(BackgroundJobNames.TournamentGroundsRollover, executionService.JobName);
        Assert.StartsWith("tournament-grounds-progression:", executionService.BusinessKey);
        Assert.Equal(1, executionService.RunCount);
        Assert.Equal(1, tournamentGrounds.EnsureUpcomingCalls);
        Assert.Equal(1, tournamentGrounds.AdvanceDueCalls);
    }

    [Fact]
    public async Task Execute_WhenDisabled_DoesNotRunExecutionService()
    {
        var tournamentGrounds = new CapturingTournamentGroundsService();
        var executionService = new CapturingBackgroundJobExecutionService();
        var job = new TournamentGroundsProgressionJob(
            tournamentGrounds,
            executionService,
            Options.Create(new TournamentGroundsOptions { Enabled = false }),
            NullLogger<TournamentGroundsProgressionJob>.Instance);

        await job.Execute(new FakeJobExecutionContext());

        Assert.Equal(0, executionService.RunCount);
        Assert.Equal(0, tournamentGrounds.EnsureUpcomingCalls);
        Assert.Equal(0, tournamentGrounds.AdvanceDueCalls);
    }

    private sealed class CapturingBackgroundJobExecutionService : IBackgroundJobExecutionService
    {
        public string? JobName { get; private set; }
        public string? BusinessKey { get; private set; }
        public int RunCount { get; private set; }

        public async Task<bool> RunOnceAsync(
            string jobName,
            string businessKey,
            Func<CancellationToken, Task> execute,
            CancellationToken cancellationToken)
        {
            JobName = jobName;
            BusinessKey = businessKey;
            RunCount++;
            await execute(cancellationToken);
            return true;
        }
    }

    private sealed class CapturingTournamentGroundsService : ITournamentGroundsService
    {
        public int EnsureUpcomingCalls { get; private set; }
        public int AdvanceDueCalls { get; private set; }

        public Task EnsureUpcomingTournamentsAsync(CancellationToken cancellationToken)
        {
            EnsureUpcomingCalls++;
            return Task.CompletedTask;
        }

        public Task AdvanceDueTournamentsAsync(CancellationToken cancellationToken)
        {
            AdvanceDueCalls++;
            return Task.CompletedTask;
        }

        public Task<StartDevelopmentTournamentResult> StartDevelopmentTournamentAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<TournamentGroundsStatus> GetStatusAsync(Guid characterId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<TournamentDetails?> GetDetailsAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<TournamentHistoryEntry>> GetHistoryAsync(Guid characterId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<TournamentHallOfFameEntry>> GetHallOfFameAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<TournamentSeasonLeaderboardEntry>> GetSeasonLeaderboardAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<TournamentBracket?> GetBracketAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<CombatResult?> GetMatchReplayAsync(Guid characterId, Guid tournamentId, Guid matchId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IReadOnlyList<TournamentRewardTier> GetRewardTiers()
            => throw new NotSupportedException();

        public Task<IReadOnlyList<TournamentRewardGrantEntry>> GetRewardsAsync(Guid characterId, Guid? tournamentId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<RegisterTournamentResult?> RegisterAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<TournamentTeamActionResult?> UpdateLoadoutAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<WithdrawTournamentResult?> WithdrawAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<CreateTournamentTeamResult?> CreateTeamAsync(Guid characterId, Guid tournamentId, string name, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<TournamentTeamActionResult?> InviteToTeamAsync(Guid characterId, Guid tournamentId, Guid teamId, Guid invitedParticipantId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<TournamentTeamActionResult?> AcceptTeamInviteAsync(Guid characterId, Guid inviteId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<TournamentTeamActionResult?> ApplyToTeamAsync(Guid characterId, Guid tournamentId, Guid teamId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<TournamentTeamActionResult?> AcceptTeamApplicationAsync(Guid characterId, Guid applicationId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<TournamentTeamActionResult?> KickTeamMemberAsync(Guid characterId, Guid tournamentId, Guid teamId, Guid participantId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ClaimTournamentRewardsResult> ClaimRewardsAsync(Guid characterId, Guid? tournamentId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeJobExecutionContext : IJobExecutionContext
    {
        private readonly Dictionary<object, object> _data = [];

        public IScheduler Scheduler => null!;
        public ITrigger Trigger { get; } = TriggerBuilder.Create()
            .WithIdentity("pvp.tournament-grounds-progression.trigger", BackgroundJobGroups.PvP)
            .Build();
        public ICalendar Calendar => null!;
        public bool Recovering => false;
        public TriggerKey RecoveringTriggerKey => null!;
        public int RefireCount => 0;
        public JobDataMap MergedJobDataMap { get; } = [];
        public IJobDetail JobDetail { get; } = JobBuilder.Create<TournamentGroundsProgressionJob>()
            .WithIdentity(BackgroundJobNames.TournamentGroundsRollover, BackgroundJobGroups.PvP)
            .Build();
        public IJob JobInstance => null!;
        public DateTimeOffset FireTimeUtc { get; } = new(2026, 7, 2, 5, 30, 0, TimeSpan.Zero);
        public DateTimeOffset? ScheduledFireTimeUtc { get; } = new DateTimeOffset(2026, 7, 2, 5, 30, 0, TimeSpan.Zero);
        public DateTimeOffset? PreviousFireTimeUtc => null;
        public DateTimeOffset? NextFireTimeUtc => null;
        public string FireInstanceId => "test-fire-instance";
        public object? Result { get; set; }
        public TimeSpan JobRunTime => TimeSpan.Zero;
        public CancellationToken CancellationToken => CancellationToken.None;

        public void Put(object key, object objectValue)
        {
            _data[key] = objectValue;
        }

        public object? Get(object key)
        {
            return _data.GetValueOrDefault(key);
        }
    }
}
