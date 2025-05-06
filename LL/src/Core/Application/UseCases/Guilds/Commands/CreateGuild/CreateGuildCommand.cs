using Application.Interfaces.Services.LL;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.CreateGuild;
public record CreateGuildCommand(Guid CharacterId, string Name) : IRequest<Response<bool>>;

public record CreateGuildCommandHandler : IRequestHandler<CreateGuildCommand, Response<bool>>
{
    private readonly IGuildService _guildService;

    public CreateGuildCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task<Response<bool>> Handle(CreateGuildCommand request, CancellationToken cancellationToken)
    {
        return await _guildService.CreateAsync(request.CharacterId, request.Name, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Could not create guild.");
    }
}
