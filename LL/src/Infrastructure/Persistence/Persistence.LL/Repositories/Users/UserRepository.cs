using Application.Common.Interfaces;
using Domain.Models.Users;

namespace Persistence.LL.Repositories.Users;
public class UserRepository : IUserRepository
{
    private readonly IDbContext _context;

    public UserRepository(IDbContext context)
    {
        _context = context;
    }

    // inheritdoc />
    public bool DoesUserExist(string email)
    {
        return _context.Users.Any(x => x.Email == email);
    }
}