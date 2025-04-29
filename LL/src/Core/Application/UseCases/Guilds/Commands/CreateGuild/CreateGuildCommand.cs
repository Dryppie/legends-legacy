using Application.Interfaces.Services.LL;
using MediatR;

namespace Application.UseCases.Guilds.Commands.CreateGuild;
public record CreateGuildCommand(Guid CharacterId, string Name) : IRequest;

public record CreateGuildCommandHandler : IRequestHandler<CreateGuildCommand>
{
    private readonly IGuildService _guildService;

    public CreateGuildCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task Handle(CreateGuildCommand request, CancellationToken cancellationToken)
    {
        await _guildService.CreateAsync(request.CharacterId, request.Name, cancellationToken);
    }
}
