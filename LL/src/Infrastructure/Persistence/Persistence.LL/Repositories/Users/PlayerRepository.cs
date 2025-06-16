using Application.Common.Interfaces;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Users;
public class PlayerRepository : IPlayerRepository
{
    private readonly IDbContext _context;

    public PlayerRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetOnlinePlayerCountAsync(CancellationToken cancellationToken) => await _context.CharacterActions
            .Where(ca => ca.UpdatedAt > DateTimeOffset.UtcNow.AddMinutes(-20))
            .CountAsync(cancellationToken);
}
