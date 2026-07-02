namespace Application.Interfaces.Services.LL.Colosseum;

public interface ITournamentLockService
{
    Task LockTournamentScheduleAsync(CancellationToken cancellationToken);
    Task LockTournamentAsync(Guid tournamentId, CancellationToken cancellationToken);
}
