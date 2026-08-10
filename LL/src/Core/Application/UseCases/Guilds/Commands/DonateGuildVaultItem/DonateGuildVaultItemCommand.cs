using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.DonateGuildVaultItem;

public record DonateGuildVaultItemCommand(Guid CharacterId, Guid EquipmentInstanceId) : ICommand<Response<bool>>;

public class DonateGuildVaultItemCommandHandler : IRequestHandler<DonateGuildVaultItemCommand, Response<bool>>
{
    private readonly IGuildVaultService _vault;
    private readonly IGameEventPublisher _events;
    private readonly IGuildService _guild;

    public DonateGuildVaultItemCommandHandler(IGuildVaultService vault, IGameEventPublisher events, IGuildService guild)
    {
        _vault = vault;
        _events = events;
        _guild = guild;
    }

    public async Task<Response<bool>> Handle(DonateGuildVaultItemCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guild.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        var result = await _vault.DonateAsync(request.CharacterId, request.EquipmentInstanceId, cancellationToken);
        if (!result.Succeeded) return Response<bool>.Fail(result.Error ?? "Failed to donate equipment.");

        if (guild is not null)
            await _events.PublishAsync(new Audience.Guild(guild.Id), new GuildStateChangedMsg(guild.Id));
        return Response<bool>.Success(true);
    }
}
