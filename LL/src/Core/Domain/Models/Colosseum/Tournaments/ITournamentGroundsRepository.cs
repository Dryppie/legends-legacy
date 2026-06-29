using Domain.Models.Entities.Characters;

namespace Domain.Models.Colosseum.Tournaments;

public interface ITournamentGroundsRepository
{
    IQueryable<TournamentDefinition> Definitions { get; }
    IQueryable<TournamentInstance> Tournaments { get; }
    IQueryable<TournamentTeam> Teams { get; }
    IQueryable<TournamentTeamApplication> TeamApplications { get; }
    IQueryable<TournamentTeamInvite> TeamInvites { get; }
    IQueryable<TournamentParticipant> Participants { get; }
    IQueryable<TournamentCombatSnapshot> CombatSnapshots { get; }
    IQueryable<TournamentCombatReplay> CombatReplays { get; }
    IQueryable<TournamentRound> Rounds { get; }
    IQueryable<TournamentMatch> Matches { get; }
    IQueryable<TournamentRewardGrant> RewardGrants { get; }
    IQueryable<Character> Characters { get; }
    IQueryable<ColosseumMatchResult> ColosseumMatches { get; }

    Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
        where TEntity : class;

    ValueTask<TEntity?> FindAsync<TEntity>(object?[] keyValues, CancellationToken cancellationToken)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<ITournamentGroundsTransaction> BeginTransactionIfNeededAsync(CancellationToken cancellationToken);
    Task ExecuteTournamentAdvisoryLockAsync(long lockId, CancellationToken cancellationToken);
}

public interface ITournamentGroundsTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
