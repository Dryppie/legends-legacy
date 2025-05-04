using Application.Authorization.Interfaces;
using Application.Common.Responses;
using MediatR;

namespace Application.UseCases.Users.Commands.BindGoogle;
public record BindGoogleCommand(Guid UserId, string IdToken)
           : IRequest<Response<Unit>>;

public class BindGoogleCommandHandler
    : IRequestHandler<BindGoogleCommand, Response<Unit>>
{
    private readonly IGoogleAuthService _google;

    public BindGoogleCommandHandler(IGoogleAuthService google) => _google = google;

    public async Task<Response<Unit>> Handle(BindGoogleCommand c, CancellationToken ct)
    {
        var success = await _google.BindAsync(c.UserId, c.IdToken, ct);
        return success
             ? Response<Unit>.Success(Unit.Value)
             : Response<Unit>.Fail("That Google account is already linked to another user.");
    }
}
