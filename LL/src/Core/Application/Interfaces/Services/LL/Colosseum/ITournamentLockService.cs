namespace Application.Interfaces.Services.LL.Colosseum;

public interface ITournamentLockService
{
    Task LockTournamentAsync(Guid tournamentId, CancellationToken cancellationToken);
}
