using Application.Interfaces.Services.LL;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.RejectApplication;
public record RejectApplicationCommand(Guid CharacterId, string ApplicationCharacterId) : IRequest<Response<bool>>;
public class RejectApplicationCommandHandler : IRequestHandler<RejectApplicationCommand, Response<bool>>
{
    private readonly IGuildService _guildService;

    public RejectApplicationCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task<Response<bool>> Handle(RejectApplicationCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ApplicationCharacterId, out var applicationCharacterId)) return Response<bool>.Fail("Invalid character");

        return await _guildService.RejectApplicationAsync(request.CharacterId, applicationCharacterId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to reject application");
    }
}