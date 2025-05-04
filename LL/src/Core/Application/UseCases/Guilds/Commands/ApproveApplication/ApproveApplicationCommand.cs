using Application.Interfaces.Services.LL;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ApproveApplication;
public record ApproveApplicationCommand(Guid CharacterId, string ApplicationCharacterId) : IRequest;
public class ApproveApplicationCommandHandler : IRequestHandler<ApproveApplicationCommand>
{
    private readonly IGuildService _guildService;

    public ApproveApplicationCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task Handle(ApproveApplicationCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ApplicationCharacterId, out var applicationCharacterId))
            throw new ArgumentException("Invalid ApplicationCharacterId");

        await _guildService.ApproveApplicationAsync(request.CharacterId, applicationCharacterId, cancellationToken);
    }
}