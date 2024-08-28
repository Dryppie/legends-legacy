using Application.Authorization.Interfaces;
using MediatR;

namespace Application.UseCases.Authorization.Queries.ValidateToken;
/// <summary>
/// Validate whether a token is valid
/// </summary>
/// <param name="Token"></param>
public record ValidateTokenQuery(string Token) : IRequest<bool>;

public class ValidateTokenQueryHandler : IRequestHandler<ValidateTokenQuery, bool>
{
    private readonly IJwtGenerator _jwtGenerator;

    public ValidateTokenQueryHandler(IJwtGenerator jwtGenerator)
    {
        _jwtGenerator = jwtGenerator;
    }

    public Task<bool> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_jwtGenerator.ValidateAccessToken(request.Token));
    }
}