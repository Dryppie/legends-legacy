using Application.Interfaces.Services.LL;
using Common.Authorization.Security;
using Common.Exceptions;
using Domain.Models.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Services.LL.Users;
public class UserService : IUserService
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;
    private readonly UserManager<AppUser> _userManager;

    public UserService(IMediator mediator, IUserRepository userRepository, UserManager<AppUser> userManager)
    {
        _mediator = mediator;
        _userRepository = userRepository;
        _userManager = userManager;
    }

    // inheritdoc />
    public async Task<AuthInfo> Login(string email, string password)
    {
        // Throw NotFound exception if no user is found by the specified email
        var user = await _userManager.FindByEmailAsync(email) ?? throw new NotFoundException();

        // Return the found user if the given password is valid for said user, else throw NotFound exception
        if (await _userManager.CheckPasswordAsync(user, password))
        {
            return new AuthInfo
            {
                IsValid = true,
                Id = user.Id,
                Name = user.UserName!,
            };
        }

        throw new NotFoundException();
    }

    public async Task<AuthInfo> Register(string username, string email, string password)
    {
        if (_userRepository.DoesUserExist(email)) throw new Exception("User exists already");

        var user = new AppUser { UserName = username, Email = email };

        var result = await _userManager.CreateAsync(user, password);

        return new AuthInfo
        {
            IsValid = true,
            Id = user.Id,
            Name = user.UserName,
        };
    }
}