using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ApproveApplication;
public record ApproveApplicationCommand(Guid CharacterId, string ApplicationCharacterId) : ICommand<Response<bool>>;
public class ApproveApplicationCommandHandler : IRequestHandler<ApproveApplicationCommand, Response<bool>>
{
    private readonly IGuildService _guildService;

    public ApproveApplicationCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task<Response<bool>> Handle(ApproveApplicationCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ApplicationCharacterId, out var applicationCharacterId)) return Response<bool>.Fail("Invalid character");

        return await _guildService.ApproveApplicationAsync(request.CharacterId, applicationCharacterId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to approve application");
    }
}