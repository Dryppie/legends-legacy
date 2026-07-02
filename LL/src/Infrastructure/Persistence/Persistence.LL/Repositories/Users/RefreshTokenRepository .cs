using Application.Common.Interfaces;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Users;
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbContext _context;
    private readonly ITokenHasher _hasher;
    public RefreshTokenRepository(IDbContext context, ITokenHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public void Add(RefreshToken token)
    {
        _context.RefreshTokens.Add(token);
    }

    public async Task<RefreshToken?> FindAsync(string plaintext, CancellationToken cancellationToken)
    {
        var hash = _hasher.Hash(plaintext);

        return await _context.RefreshTokens
                        .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
    }

    public async Task RevokeActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedUtc == null && t.ExpiresUtc >= now)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedUtc = now;
        }
    }
}
