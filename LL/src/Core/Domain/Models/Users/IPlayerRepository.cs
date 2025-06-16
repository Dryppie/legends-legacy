
namespace Domain.Models.Users;
public interface IPlayerRepository
{
    Task<int> GetOnlinePlayerCountAsync(CancellationToken cancellationToken);
}
