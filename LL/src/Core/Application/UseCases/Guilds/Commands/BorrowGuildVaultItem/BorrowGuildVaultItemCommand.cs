using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.BorrowGuildVaultItem;

public record BorrowGuildVaultItemCommand(Guid CharacterId, Guid VaultItemId) : ICommand<Response<bool>>;

public class BorrowGuildVaultItemCommandHandler : IRequestHandler<BorrowGuildVaultItemCommand, Response<bool>>
{
    private readonly IGuildVaultService _vault;
    private readonly IGuildService _guild;
    private readonly IGameRealtimeBroadcaster _events;
    public BorrowGuildVaultItemCommandHandler(IGuildVaultService vault, IGuildService guild, IGameRealtimeBroadcaster events)
    {
        _vault = vault;
        _guild = guild;
        _events = events;
    }

    public async Task<Response<bool>> Handle(BorrowGuildVaultItemCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guild.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        var result = await _vault.BorrowAsync(request.CharacterId, request.VaultItemId, cancellationToken);
        if (!result.Succeeded) return Response<bool>.Fail(result.Error ?? "Failed to borrow equipment.");
        if (guild is not null)
            await _events.PublishAsync(new Audience.Guild(guild.Id), new GuildStateChanged(guild.Id, request.CharacterId, true), nameof(BorrowGuildVaultItemCommandHandler), cancellationToken);
        return Response<bool>.Success(true);
    }
}
