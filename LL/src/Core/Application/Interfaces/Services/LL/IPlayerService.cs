namespace Application.Interfaces.Services.LL;
public interface IPlayerService
{
    /// <summary>
    /// Gets the count of online players.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The number of online players.</returns>
    Task<int> GetOnlinePlayerCountAsync(CancellationToken cancellationToken);
}
