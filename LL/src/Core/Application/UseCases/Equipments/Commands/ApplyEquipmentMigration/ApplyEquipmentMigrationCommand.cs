using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Commands.ApplyEquipmentMigration;

public sealed record ApplyEquipmentMigrationCommand(Guid OperationId, EquipmentMigrationTarget Target, string SourceHash, string DefinitionId, string ActorId, int? TargetBalanceVersion = null, string? ExpectedResultHash = null) : ICommand<Response<EquipmentMigrationReceipt>>;

public sealed class ApplyEquipmentMigrationCommandHandler(IEquipmentMigrationService service) : IRequestHandler<ApplyEquipmentMigrationCommand, Response<EquipmentMigrationReceipt>>
{
    public async Task<Response<EquipmentMigrationReceipt>> Handle(ApplyEquipmentMigrationCommand request, CancellationToken ct)
    {
        return Response<EquipmentMigrationReceipt>.Success(await service.ApplyAsync(request.OperationId, request.Target, request.SourceHash, request.DefinitionId, request.ActorId, ct, request.TargetBalanceVersion, request.ExpectedResultHash));
    }
}
