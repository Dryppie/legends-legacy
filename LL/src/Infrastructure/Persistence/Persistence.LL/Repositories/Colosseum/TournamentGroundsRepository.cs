using Domain.Models.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Entities.Characters;
using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Persistence.LL.Repositories.Colosseum;

public sealed class TournamentGroundsRepository(LLDbContext context) : ITournamentGroundsRepository
{
    public IQueryable<TournamentDefinition> Definitions => context.TournamentDefinitions;
    public IQueryable<TournamentInstance> Tournaments => context.ArenaTournaments;
    public IQueryable<TournamentTeam> Teams => context.TournamentTeams;
    public IQueryable<TournamentTeamApplication> TeamApplications => context.TournamentTeamApplications;
    public IQueryable<TournamentTeamInvite> TeamInvites => context.TournamentTeamInvites;
    public IQueryable<TournamentParticipant> Participants => context.TournamentParticipants;
    public IQueryable<TournamentCombatSnapshot> CombatSnapshots => context.TournamentCombatSnapshots;
    public IQueryable<TournamentCombatReplay> CombatReplays => context.TournamentCombatReplays;
    public IQueryable<TournamentCombatReplayArtifact> CombatReplayArtifacts => context.TournamentCombatReplayArtifacts;
    public IQueryable<TournamentRound> Rounds => context.TournamentRounds;
    public IQueryable<TournamentMatch> Matches => context.TournamentMatches;
    public IQueryable<TournamentRewardGrant> RewardGrants => context.TournamentRewardGrants;
    public IQueryable<Character> Characters => context.Characters;
    public IQueryable<ColosseumMatchResult> ColosseumMatches => context.ColosseumMatches;
    public IQueryable<AccountRestriction> AccountRestrictions => context.AccountRestrictions;

    public async Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
        where TEntity : class
    {
        await context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public ValueTask<TEntity?> FindAsync<TEntity>(object?[] keyValues, CancellationToken cancellationToken)
        where TEntity : class
    {
        return context.Set<TEntity>().FindAsync(keyValues, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);

    public async Task<ITournamentGroundsTransaction> BeginTransactionIfNeededAsync(CancellationToken cancellationToken)
    {
        if (context.CurrentTransaction is not null)
        {
            return new TournamentGroundsTransaction(null);
        }

        return new TournamentGroundsTransaction(await context.BeginTransactionAsync(cancellationToken));
    }

    public async Task ExecuteTournamentAdvisoryLockAsync(long lockId, CancellationToken cancellationToken)
    {
        await context.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})",
            cancellationToken,
            lockId);
    }

    private sealed class TournamentGroundsTransaction(IDbContextTransaction? transaction) : ITournamentGroundsTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken)
            => transaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask;

        public ValueTask DisposeAsync()
            => transaction?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
