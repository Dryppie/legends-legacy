using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;

namespace Application.UseCases.Equipments.Commands.SaveEquipmentLoadout;

public sealed record SaveEquipmentLoadoutCommand(Guid CharacterId, Guid? Id, string Name) : ICommand<Response<bool>>;

public sealed class SaveEquipmentLoadoutCommandHandler(IEquipmentLoadoutService service, IGameEventOutbox outbox) : IRequestHandler<SaveEquipmentLoadoutCommand, Response<bool>>
{
    public async Task<Response<bool>> Handle(SaveEquipmentLoadoutCommand request, CancellationToken ct)
    {
        var result = await service.SaveAsync(request.CharacterId, request.Id, request.Name, ct);
        if (!result.Succeeded) return Response<bool>.Fail(result.ErrorMessage ?? "Equipment loadout could not be updated.");
        await outbox.EnqueueAsync(GameEventTypes.EquipmentChanged, new EquipmentChangedPayload(request.CharacterId), request.CharacterId, null, ct);
        return Response<bool>.Success(true);
    }
}
