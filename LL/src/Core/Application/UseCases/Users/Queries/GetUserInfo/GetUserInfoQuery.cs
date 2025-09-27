using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Users.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Users.Queries.GetUserInfo;

public record GetUserInfoQuery(Guid UserId) : IQuery<Response<UserInfoDto>>;

public class GetUserInfoQueryHandler : IRequestHandler<GetUserInfoQuery, Response<UserInfoDto>>
{
    private readonly IUserService _userService;
    
    public GetUserInfoQueryHandler(IUserService userService)
    {
        _userService = userService;
    }
    public async Task<Response<UserInfoDto>> Handle(GetUserInfoQuery request, CancellationToken cancellationToken)
    {
        var userInfo = await _userService.GetUserInfo(request.UserId, cancellationToken);
        if (userInfo == null) return Response<UserInfoDto>.Fail("Failed getting user info.");

        var userInfoDto = new UserInfoDto
        {
            Email = userInfo.Email,
            IsRegisteredUser = userInfo.IsRegisteredUser,
            IsGmailBound = userInfo.IsGmailBound,
            IsNameEdited = userInfo.IsNameEdited,
        };

        return Response<UserInfoDto>.Success(userInfoDto);
    }
}
