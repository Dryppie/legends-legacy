using Application.Interfaces.Services.LL;
using MediatR;

namespace Application.UseCases.Guilds.Commands.RejectApplication;
public record RejectApplicationCommand(Guid CharacterId, string ApplicationCharacterId) : IRequest;
public class RejectApplicationCommandHandler : IRequestHandler<RejectApplicationCommand>
{
    private readonly IGuildService _guildService;

    public RejectApplicationCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task Handle(RejectApplicationCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ApplicationCharacterId, out var applicationCharacterId))
            throw new ArgumentException("Invalid ApplicationCharacterId");

        await _guildService.RejectApplicationAsync(request.CharacterId, applicationCharacterId, cancellationToken);
    }
}