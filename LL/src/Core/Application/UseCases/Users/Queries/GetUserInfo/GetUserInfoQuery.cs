using Application.Interfaces.Services.LL;
using Application.UseCases.Users.Dtos;
using MediatR;

namespace Application.UseCases.Users.Queries.GetUserInfo;

public record GetUserInfoQuery(Guid UserId) : IRequest<UserInfoDto>;

public class GetUserInfoQueryHandler : IRequestHandler<GetUserInfoQuery, UserInfoDto>
{
    private readonly IUserService _userService;
    
    public GetUserInfoQueryHandler(IUserService userService)
    {
        _userService = userService;
    }
    public async Task<UserInfoDto> Handle(GetUserInfoQuery request, CancellationToken cancellationToken)
    {
        var userInfo = await _userService.GetUserInfo(request.UserId, cancellationToken);

        var userInfoDto = new UserInfoDto
        {
            Email = userInfo.Email,
            IsRegisteredUser = userInfo.IsRegisteredUser,
            IsGmailBound = userInfo.IsGmailBound,
        };

        return userInfoDto;
    }
}
