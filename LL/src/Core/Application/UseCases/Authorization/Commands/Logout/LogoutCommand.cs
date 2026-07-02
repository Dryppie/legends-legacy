using Application.Authorization.Interfaces;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Authorization.Commands.Logout;

public record LogoutCommand(string RefreshToken) : ICommand<Response<Unit>>;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Response<Unit>>
{
    private readonly IJwtGenerator _jwtGenerator;

    public LogoutCommandHandler(IJwtGenerator jwtGenerator)
    {
        _jwtGenerator = jwtGenerator;
    }

    public async Task<Response<Unit>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _jwtGenerator.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        return Response<Unit>.Success(Unit.Value);
    }
}
