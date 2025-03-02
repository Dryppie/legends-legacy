using Application.Authorization.Interfaces;
using Application.Common.Responses;
using Common.Authorization.Security;
using MediatR;

namespace Application.UseCases.Authorization.Commands.CreateNewTokens;
/// <summary>
/// Generate new tokens
/// </summary>
/// <param name="Token"></param>
public record CreateNewTokensCommand(string RefreshToken) : IRequest<Response<Tokens>>;

public class CreateNewTokensCommandHandler : IRequestHandler<CreateNewTokensCommand, Response<Tokens>>
{
    private readonly IJwtGenerator _jwtGenerator;

    public CreateNewTokensCommandHandler(IJwtGenerator jwtGenerator)
    {
        _jwtGenerator = jwtGenerator;
    }

    public async Task<Response<Tokens>> Handle(CreateNewTokensCommand request, CancellationToken cancellationToken)
    {
        var tokens = await Task.FromResult(_jwtGenerator.RefreshTokens(request.RefreshToken));
        return Response<Tokens>.Success(tokens);
    }
}