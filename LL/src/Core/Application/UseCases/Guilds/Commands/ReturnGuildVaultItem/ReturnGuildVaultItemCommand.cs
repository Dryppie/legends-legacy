using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ReturnGuildVaultItem;

public record ReturnGuildVaultItemCommand(Guid CharacterId, Guid VaultItemId) : ICommand<Response<bool>>;

public class ReturnGuildVaultItemCommandHandler : IRequestHandler<ReturnGuildVaultItemCommand, Response<bool>>
{
    private readonly IGuildVaultService _vault;
    private readonly IGameEventOutbox _outbox;
    public ReturnGuildVaultItemCommandHandler(IGuildVaultService vault, IGameEventOutbox outbox)
    {
        _vault = vault;
        _outbox = outbox;
    }

    public async Task<Response<bool>> Handle(ReturnGuildVaultItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _vault.ReturnAsync(request.CharacterId, request.VaultItemId, cancellationToken);
        if (!result.Succeeded) return Response<bool>.Fail(result.Error ?? "Failed to return equipment.");
        await _outbox.EnqueueAsync(GameEventTypes.EquipmentChanged, new EquipmentChangedPayload(request.CharacterId), request.CharacterId, null, cancellationToken);
        return Response<bool>.Success(true);
    }
}
