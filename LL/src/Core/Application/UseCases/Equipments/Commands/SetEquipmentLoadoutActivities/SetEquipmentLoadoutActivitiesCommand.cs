using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;

namespace Application.UseCases.Equipments.Commands.SetEquipmentLoadoutActivities;

public sealed record SetEquipmentLoadoutActivitiesCommand(Guid CharacterId, Guid Id, IReadOnlyList<Domain.Models.Essences.EssenceCombatActivity> Activities) : ICommand<Response<bool>>;

public sealed class SetEquipmentLoadoutActivitiesCommandHandler(IEquipmentLoadoutService service, IGameEventOutbox outbox) : IRequestHandler<SetEquipmentLoadoutActivitiesCommand, Response<bool>>
{
    public async Task<Response<bool>> Handle(SetEquipmentLoadoutActivitiesCommand request, CancellationToken ct)
    {
        var result = await service.SetActivitiesAsync(request.CharacterId, request.Id, request.Activities, ct);
        if (!result.Succeeded) return Response<bool>.Fail(result.ErrorMessage ?? "Equipment loadout could not be updated.");
        await outbox.EnqueueAsync(GameEventTypes.EquipmentChanged, new EquipmentChangedPayload(request.CharacterId), request.CharacterId, null, ct);
        return Response<bool>.Success(true);
    }
}
