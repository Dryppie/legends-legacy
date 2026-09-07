using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;

namespace Application.UseCases.Equipments.Commands.ApplyEquipmentLoadout;

public sealed record ApplyEquipmentLoadoutCommand(Guid CharacterId, Guid Id) : ICommand<Response<bool>>;

public sealed class ApplyEquipmentLoadoutCommandHandler(IEquipmentLoadoutService service, IGameEventOutbox outbox) : IRequestHandler<ApplyEquipmentLoadoutCommand, Response<bool>>
{
    public async Task<Response<bool>> Handle(ApplyEquipmentLoadoutCommand request, CancellationToken ct)
    {
        var result = await service.ApplyAsync(request.CharacterId, request.Id, ct);
        if (!result.Succeeded) return Response<bool>.Fail(result.ErrorMessage ?? "Equipment loadout could not be updated.");
        await outbox.EnqueueAsync(GameEventTypes.EquipmentChanged, new EquipmentChangedPayload(request.CharacterId), request.CharacterId, null, ct);
        return Response<bool>.Success(true);
    }
}
