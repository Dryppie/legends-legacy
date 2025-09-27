using Application.Authorization.Interfaces;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Authorization.Queries.ValidateToken;
/// <summary>
/// Validate whether a token is valid
/// </summary>
/// <param name="Token"></param>
public record ValidateTokenQuery(string Token) : IQuery<Response<bool>>;

public class ValidateTokenQueryHandler : IRequestHandler<ValidateTokenQuery, Response<bool>>
{
    private readonly IJwtGenerator _jwtGenerator;

    public ValidateTokenQueryHandler(IJwtGenerator jwtGenerator)
    {
        _jwtGenerator = jwtGenerator;
    }

    public async Task<Response<bool>> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        return await _jwtGenerator.ValidateAccessToken(request.Token)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed token validation");

    }
}