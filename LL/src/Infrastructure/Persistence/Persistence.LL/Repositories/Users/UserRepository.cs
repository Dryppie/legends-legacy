using Application.Common.Interfaces;
using Domain.Models.Users;
using Persistence.LL.Interfaces;

namespace Persistence.LL.Repositories.Users;
public class UserRepository : IUserRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public UserRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // inheritdocs />
    public bool DoesUserExist(string email)
    {
        return _unitOfWork.Context.Users.Any(x => x.Email == email);
    }
}