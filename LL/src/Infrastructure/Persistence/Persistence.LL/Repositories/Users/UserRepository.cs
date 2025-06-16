using Application.Common.Interfaces;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Users;
public class UserRepository : IUserRepository
{
    private readonly IDbContext _context;

    public UserRepository(IDbContext unitOfWork)
    {
        _context = unitOfWork;
    }

    public async Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        await _context.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<AppUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _context.Users.FindAsync([id], cancellationToken);

    public async Task<bool> AddAsync(AppUser user, CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == user.Username.ToLower(), cancellationToken)) return false;

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserInfo?> GetUserInfo(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users.Include(u => u.ExternalLogins).FirstOrDefaultAsync(x => x.Id.Equals(userId), cancellationToken);
        //TODO: Implement exception
        if (user == null) return null;

        return new UserInfo
        {
            Email = user.Email ?? string.Empty,
            IsRegisteredUser = !user.IsGuest,
            IsGmailBound = user.ExternalLogins.Count > 0,
            IsNameEdited = user.IsNameEdited,
        };
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await _context.SaveChangesAsync(cancellationToken);

    public async Task<AppUser?> GetUserById(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Users.FindAsync([userId], cancellationToken);
    }
}