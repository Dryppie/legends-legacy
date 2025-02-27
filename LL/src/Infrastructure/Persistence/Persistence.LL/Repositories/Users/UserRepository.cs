using Application.Common.Interfaces;
using Domain.Models.Users;

namespace Persistence.LL.Repositories.Users;
public class UserRepository : IUserRepository
{
    private readonly IDbContext _context;

    public UserRepository(IDbContext unitOfWork)
    {
        _context = unitOfWork;
    }

    // inheritdocs />
    public bool DoesEmailExist(string email)
    {
        return _context.Users.Any(x => x.Email == email);
    }
    
    // inheritdocs />
    public bool DoesUsernameExist(string username)
    {
        // It's not possible to use string.Equals(x, StringComparison.) with EF Core, as it can not turn it into SQL
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
        return _context.Users.Any(x => x.UserName != null && x.UserName.ToLower() == username.ToLower());
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
    }

    // inheritdocs />
    public bool DoesGuestExist(string userId)
    {
        return _context.Users.Any(x => x.Id == userId);
    }
}