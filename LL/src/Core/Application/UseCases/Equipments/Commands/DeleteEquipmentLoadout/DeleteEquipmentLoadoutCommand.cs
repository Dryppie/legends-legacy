using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;

namespace Application.UseCases.Equipments.Commands.DeleteEquipmentLoadout;

public sealed record DeleteEquipmentLoadoutCommand(Guid CharacterId, Guid Id) : ICommand<Response<bool>>;

public sealed class DeleteEquipmentLoadoutCommandHandler(IEquipmentLoadoutService service, IGameEventOutbox outbox) : IRequestHandler<DeleteEquipmentLoadoutCommand, Response<bool>>
{
    public async Task<Response<bool>> Handle(DeleteEquipmentLoadoutCommand request, CancellationToken ct)
    {
        var result = await service.DeleteAsync(request.CharacterId, request.Id, ct);
        if (!result.Succeeded) return Response<bool>.Fail(result.ErrorMessage ?? "Equipment loadout could not be updated.");
        await outbox.EnqueueAsync(GameEventTypes.EquipmentChanged, new EquipmentChangedPayload(request.CharacterId), request.CharacterId, null, ct);
        return Response<bool>.Success(true);
    }
}
