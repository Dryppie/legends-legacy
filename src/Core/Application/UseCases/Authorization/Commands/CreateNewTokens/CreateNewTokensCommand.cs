using Application.Authorization.Interfaces;
using Common.Authorization.Security;
using MediatR;

namespace Application.UseCases.Authorization.Commands.CreateNewTokens;
/// <summary>
/// Generate new tokens
/// </summary>
/// <param name="Token"></param>
public record CreateNewTokensCommand(string RefreshToken) : IRequest<Tokens>;

public class CreateNewTokensCommandHandler : IRequestHandler<CreateNewTokensCommand, Tokens>
{
    private readonly IJwtGenerator _jwtGenerator;

    public CreateNewTokensCommandHandler(IJwtGenerator jwtGenerator)
    {
        _jwtGenerator = jwtGenerator;
    }

    public Task<Tokens> Handle(CreateNewTokensCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_jwtGenerator.RefreshTokens(request.RefreshToken));
    }
}