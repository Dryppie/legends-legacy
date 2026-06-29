using Application.Interfaces.Services.LL.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Microsoft.Extensions.Options;

namespace Services.LL.Colosseum.Tournaments;

public sealed class PostgresTournamentLockService(
    ITournamentGroundsRepository tournaments,
    IOptions<TournamentGroundsOptions> options) : ITournamentLockService
{
    public async Task LockTournamentAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        if (!options.Value.UsePostgresAdvisoryLocks) return;

        try
        {
            await tournaments.ExecuteTournamentAdvisoryLockAsync(BuildTournamentLockId(tournamentId), cancellationToken);
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
