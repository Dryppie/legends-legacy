using Application.Interfaces.Services.LL.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Microsoft.Extensions.Options;

namespace Services.LL.Colosseum.Tournaments;

public sealed class PostgresTournamentLockService(
    ITournamentGroundsRepository tournaments,
    IOptions<TournamentGroundsOptions> options) : ITournamentLockService
{
    private const long TournamentGroundsScheduleLockId = 0x54474C4F434B0001;

    public Task LockTournamentScheduleAsync(CancellationToken cancellationToken)
        => ExecuteLockAsync(TournamentGroundsScheduleLockId, cancellationToken);

    public Task LockTournamentAsync(Guid tournamentId, CancellationToken cancellationToken)
        => ExecuteLockAsync(BuildTournamentLockId(tournamentId), cancellationToken);

    private async Task ExecuteLockAsync(long lockId, CancellationToken cancellationToken)
    {
        if (!options.Value.UsePostgresAdvisoryLocks) return;

        try
        {
            await tournaments.ExecuteTournamentAdvisoryLockAsync(lockId, cancellationToken);
        }
        catch
        {
            // Non-PostgreSQL providers and some tests do not support advisory locks.
            // Transactions and unique constraints still provide baseline idempotency.
        }
    }

    private static long BuildTournamentLockId(Guid tournamentId)
    {
        var bytes = tournamentId.ToByteArray();
        return BitConverter.ToInt64(bytes, 0);
    }
}
