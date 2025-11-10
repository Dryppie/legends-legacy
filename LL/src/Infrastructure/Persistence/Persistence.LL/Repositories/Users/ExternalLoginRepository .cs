using Application.Common.Interfaces;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Users;
public sealed class ExternalLoginRepository : IExternalLoginRepository
{
    private readonly IDbContext _context;
    public ExternalLoginRepository(IDbContext db) => _context = db;

    public Task<ExternalLogin?> FindAsync(AuthProvider p, string id, CancellationToken cancellationToken) =>
        _context.ExternalLogins
            .Include(l => l.User)
            .SingleOrDefaultAsync(l => l.Provider == p && l.ProviderUserId == id, cancellationToken);

    public void Add(ExternalLogin login)
    {
        _context.ExternalLogins.Add(login);
    }
}