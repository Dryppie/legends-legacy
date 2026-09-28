using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Commands.RollbackEquipmentMigration;

public sealed record RollbackEquipmentMigrationCommand(Guid OperationId, string ActorId) : ICommand<Response<EquipmentMigrationReceipt>>;

public sealed class RollbackEquipmentMigrationCommandHandler(IEquipmentMigrationService service) : IRequestHandler<RollbackEquipmentMigrationCommand, Response<EquipmentMigrationReceipt>>
{
    public async Task<Response<EquipmentMigrationReceipt>> Handle(RollbackEquipmentMigrationCommand request, CancellationToken ct)
    {
        return Response<EquipmentMigrationReceipt>.Success(await service.RollbackAsync(request.OperationId, request.ActorId, ct));
    }
}
