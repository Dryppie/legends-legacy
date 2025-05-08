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

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> FindAsync(string plaintext, CancellationToken cancellationToken)
    {
        var hash = _hasher.Hash(plaintext);

        return await _context.RefreshTokens
                        .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await _context.SaveChangesAsync(cancellationToken);
}