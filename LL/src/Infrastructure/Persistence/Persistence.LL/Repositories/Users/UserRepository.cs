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

    public async Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = IdentityNormalizer.NormalizeOptional(email);
        if (normalizedEmail is null) return null;

        return await _context.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<AppUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _context.Users.FindAsync([id], cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, Guid? excludedUserId, CancellationToken cancellationToken)
    {
        var normalizedEmail = IdentityNormalizer.NormalizeOptional(email);
        if (normalizedEmail is null) return false;

        return await _context.Users.AnyAsync(
            u => u.NormalizedEmail == normalizedEmail && (!excludedUserId.HasValue || u.Id != excludedUserId.Value),
            cancellationToken);
    }

    public async Task<bool> UsernameExistsAsync(string username, Guid? excludedUserId, CancellationToken cancellationToken)
    {
        var normalizedUsername = IdentityNormalizer.NormalizeOptional(username);
        if (normalizedUsername is null) return false;

        return await _context.Users.AnyAsync(
            u => u.NormalizedUsername == normalizedUsername && (!excludedUserId.HasValue || u.Id != excludedUserId.Value),
            cancellationToken);
    }

    public async Task<bool> AddAsync(AppUser user, CancellationToken cancellationToken)
    {
        user.NormalizeIdentityFields();
        if (await UsernameExistsAsync(user.Username, null, cancellationToken)) return false;
        if (user.Email is not null && await EmailExistsAsync(user.Email, null, cancellationToken)) return false;

        _context.Users.Add(user);
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

    public async Task<AppUser?> GetUserById(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Users.FindAsync([userId], cancellationToken);
    }
}
