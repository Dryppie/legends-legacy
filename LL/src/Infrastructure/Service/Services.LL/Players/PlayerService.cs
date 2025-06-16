using Application.Interfaces.Services.LL;
using Domain.Models.Users;

namespace Services.LL.Players;
public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _playerRepository;

    public PlayerService(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<int> GetOnlinePlayerCountAsync(CancellationToken cancellationToken)
        => await _playerRepository.GetOnlinePlayerCountAsync(cancellationToken);
}
